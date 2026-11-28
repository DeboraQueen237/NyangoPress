using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NyangoPress.Infrastructure.Persistence;

/// <summary>
/// Design-time factory enabling EF Core CLI migrations for PostgreSQL.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var conn = Environment.GetEnvironmentVariable("NYANGOPRESS_PG_CONN")
                   ?? "Host=localhost;Port=5432;Database=nyangopress;Username=nyango;Password=nyango";
        optionsBuilder.UseNpgsql(conn);
        return new AppDbContext(optionsBuilder.Options);
    }
}
