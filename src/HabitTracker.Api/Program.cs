using HabitTracker.Api.Data;
using HabitTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(DatabaseConfig.GetConnectionString(builder.Configuration)));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    if (app.Configuration.GetValue("Seed:Enabled", true))
        SeedData.Apply(db, DateOnly.FromDateTime(DateTime.Now));
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ok", database = "ok" })
        : Results.Json(new { status = "error", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable));

var api = app.MapGroup("/api/habits");

api.MapGet("/", async (AppDbContext db, string? today) =>
{
    var day = ParseDate(today) ?? DateOnly.FromDateTime(DateTime.Now);
    var habits = await db.Habits.AsNoTracking().Include(h => h.CheckIns).OrderBy(h => h.Id).ToListAsync();
    return habits.Select(h => ToDto(h, day));
});

api.MapPost("/", async (AppDbContext db, CreateHabit body) =>
{
    var name = body.Name?.Trim();
    if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        return Results.BadRequest(new { error = "El nombre es obligatorio (máximo 100 caracteres)." });

    var habit = new Habit { Name = name, CreatedAt = DateTime.UtcNow };
    db.Habits.Add(habit);
    await db.SaveChangesAsync();
    return Results.Created($"/api/habits/{habit.Id}", ToDto(habit, DateOnly.FromDateTime(DateTime.Now)));
});

api.MapDelete("/{id:int}", async (AppDbContext db, int id) =>
{
    var habit = await db.Habits.Include(h => h.CheckIns).FirstOrDefaultAsync(h => h.Id == id);
    if (habit is null) return Results.NotFound();
    db.Habits.Remove(habit);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapPut("/{id:int}/checkins/{date}", async (AppDbContext db, int id, string date, string? today) =>
{
    if (ParseDate(date) is not { } day) return Results.BadRequest(new { error = "Fecha inválida (usa yyyy-MM-dd)." });
    if (!await db.Habits.AnyAsync(h => h.Id == id)) return Results.NotFound();
    if (!await db.CheckIns.AnyAsync(c => c.HabitId == id && c.Date == day))
    {
        db.CheckIns.Add(new CheckIn { HabitId = id, Date = day });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Si otra petición marcó el mismo día a la vez, el índice único evitó el duplicado y el resultado es el mismo.
            db.ChangeTracker.Clear();
            if (!await db.CheckIns.AnyAsync(c => c.HabitId == id && c.Date == day)) throw;
        }
    }
    return await UpdatedHabit(db, id, today);
});

api.MapDelete("/{id:int}/checkins/{date}", async (AppDbContext db, int id, string date, string? today) =>
{
    if (ParseDate(date) is not { } day) return Results.BadRequest(new { error = "Fecha inválida (usa yyyy-MM-dd)." });
    if (!await db.Habits.AnyAsync(h => h.Id == id)) return Results.NotFound();
    await db.CheckIns.Where(c => c.HabitId == id && c.Date == day).ExecuteDeleteAsync();
    return await UpdatedHabit(db, id, today);
});

app.Run();

static DateOnly? ParseDate(string? s) =>
    DateOnly.TryParseExact(s, "yyyy-MM-dd", out var d) ? d : null;

// Hábito con sus rachas ya recalculadas: lo devuelven marcar y desmarcar para que el cliente no recargue la lista.
static async Task<IResult> UpdatedHabit(AppDbContext db, int id, string? today)
{
    var day = ParseDate(today) ?? DateOnly.FromDateTime(DateTime.Now);
    var habit = await db.Habits.AsNoTracking().Include(h => h.CheckIns).FirstOrDefaultAsync(h => h.Id == id);
    return habit is null ? Results.NotFound() : Results.Ok(ToDto(habit, day));
}

static HabitDto ToDto(Habit h, DateOnly today)
{
    var dates = h.CheckIns.Select(c => c.Date).ToHashSet();
    var last7 = Enumerable.Range(0, 7).Select(i => dates.Contains(today.AddDays(i - 6))).ToArray();
    return new HabitDto(h.Id, h.Name, StreakCalculator.Current(dates, today), StreakCalculator.Best(dates), last7);
}

record CreateHabit(string? Name);
record HabitDto(int Id, string Name, int CurrentStreak, int BestStreak, bool[] Last7Days);

public partial class Program;
