using Microsoft.EntityFrameworkCore;

namespace backend.Data;

/// <summary>
/// AppDbContext is the bridge between our C# application and PostgreSQL.
/// It tracks changes, maps entity classes to tables, and executes SQL queries.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Entity configurations will be added here in Phase 3
    }
}
