using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Api.Data;

public class Habit
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<CheckIn> CheckIns { get; set; } = [];
}

public class CheckIn
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public DateOnly Date { get; set; }
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Habit>(e =>
        {
            e.Property(h => h.Name).IsRequired().HasMaxLength(100);
            e.HasMany(h => h.CheckIns).WithOne().HasForeignKey(c => c.HabitId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CheckIn>().HasIndex(c => new { c.HabitId, c.Date }).IsUnique();
    }
}
