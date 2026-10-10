namespace HabitTracker.Api.Data;

/// <summary>Datos de ejemplo: SQLite en el contenedor es efímero, así que se siembran al arrancar.</summary>
public static class SeedData
{
    public static void Apply(AppDbContext db, DateOnly today)
    {
        if (db.Habits.Any()) return;

        var samples = new (string Name, int[] DaysAgo)[]
        {
            ("Beber agua", [0, 1, 2, 3, 5, 6, 7, 8, 9, 10, 11, 12, 13]),
            ("Hacer ejercicio", [1, 2, 3, 4]),
            ("Leer 20 minutos", [0, 2]),
            ("Meditar", []),
        };

        foreach (var (name, daysAgo) in samples)
        {
            var habit = new Habit { Name = name, CreatedAt = DateTime.UtcNow };
            habit.CheckIns.AddRange(daysAgo.Select(d => new CheckIn { Date = today.AddDays(-d) }));
            db.Habits.Add(habit);
        }
        db.SaveChanges();
    }
}
