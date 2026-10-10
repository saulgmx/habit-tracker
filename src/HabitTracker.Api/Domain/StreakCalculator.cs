namespace HabitTracker.Api.Domain;

/// <summary>Cálculo de rachas. Lógica pura: sin dependencias de EF ni HTTP.</summary>
public static class StreakCalculator
{
    /// <summary>
    /// Racha vigente a la fecha <paramref name="today"/>. Si hoy aún no está marcado
    /// pero ayer sí, la racha sigue viva.
    /// </summary>
    public static int Current(IEnumerable<DateOnly> dates, DateOnly today)
    {
        var set = new HashSet<DateOnly>(dates);
        var cursor = set.Contains(today) ? today : today.AddDays(-1);

        var streak = 0;
        while (set.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }

    /// <summary>Mayor cantidad de días consecutivos en todo el historial.</summary>
    public static int Best(IEnumerable<DateOnly> dates)
    {
        var sorted = dates.Distinct().OrderBy(d => d).ToList();
        var best = 0;
        var run = 0;
        DateOnly? previous = null;

        foreach (var date in sorted)
        {
            run = previous is { } p && date.DayNumber - p.DayNumber == 1 ? run + 1 : 1;
            best = Math.Max(best, run);
            previous = date;
        }
        return best;
    }
}
