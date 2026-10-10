using System.Net;
using System.Net.Http.Json;
using HabitTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HabitTracker.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseSetting("Seed:Enabled", "false");
        builder.ConfigureServices(services =>
        {
            var existing = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions)).ToList();
            foreach (var d in existing) services.Remove(d);
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

public record HabitView(int Id, string Name, int CurrentStreak, int BestStreak, bool[] Last7Days);

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<HabitView> CreateAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/habits", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<HabitView>())!;
    }

    private async Task<HabitView> GetAsync(int id, string today) =>
        (await _client.GetFromJsonAsync<HabitView[]>($"/api/habits?today={today}"))!.Single(h => h.Id == id);

    [Fact]
    public async Task Health_Responde200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Crear_NombreVacio_Devuelve400()
    {
        var response = await _client.PostAsJsonAsync("/api/habits", new { name = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Marcar_ActualizaRacha()
    {
        var habit = await CreateAsync("Leer");
        foreach (var date in new[] { "2026-10-07", "2026-10-08", "2026-10-09" })
            Assert.Equal(HttpStatusCode.NoContent, (await _client.PutAsync($"/api/habits/{habit.Id}/checkins/{date}", null)).StatusCode);

        var view = await GetAsync(habit.Id, "2026-10-09");
        Assert.Equal(3, view.CurrentStreak);
        Assert.Equal(3, view.BestStreak);
        Assert.Equal([false, false, false, false, true, true, true], view.Last7Days);
    }

    [Fact]
    public async Task Marcar_DosVeces_NoDuplica()
    {
        var habit = await CreateAsync("Agua");
        await _client.PutAsync($"/api/habits/{habit.Id}/checkins/2026-10-09", null);
        await _client.PutAsync($"/api/habits/{habit.Id}/checkins/2026-10-09", null);
        Assert.Equal(1, (await GetAsync(habit.Id, "2026-10-09")).CurrentStreak);
    }

    [Fact]
    public async Task Desmarcar_QuitaElDia()
    {
        var habit = await CreateAsync("Correr");
        await _client.PutAsync($"/api/habits/{habit.Id}/checkins/2026-10-09", null);
        await _client.DeleteAsync($"/api/habits/{habit.Id}/checkins/2026-10-09");
        Assert.Equal(0, (await GetAsync(habit.Id, "2026-10-09")).CurrentStreak);
    }

    [Fact]
    public async Task Marcar_FechaInvalida_Devuelve400()
    {
        var habit = await CreateAsync("Yoga");
        var response = await _client.PutAsync($"/api/habits/{habit.Id}/checkins/no-es-fecha", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Marcar_HabitoInexistente_Devuelve404()
    {
        var response = await _client.PutAsync("/api/habits/9999/checkins/2026-10-09", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Eliminar_BorraHabitoYSusCheckins()
    {
        var habit = await CreateAsync("Temporal");
        await _client.PutAsync($"/api/habits/{habit.Id}/checkins/2026-10-09", null);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/habits/{habit.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/habits/{habit.Id}")).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.CheckIns.AnyAsync(c => c.HabitId == habit.Id));
    }
}
