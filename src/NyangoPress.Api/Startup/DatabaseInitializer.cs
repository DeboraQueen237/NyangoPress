using Microsoft.EntityFrameworkCore;
using NyangoPress.Infrastructure.Persistence;
using NyangoPress.Infrastructure.Seed;

namespace NyangoPress.Api.Startup;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applique les migrations et remplit les données de départ quand <c>Database:AutoMigrate</c> est vrai
    /// (activé dans appsettings.Development.json, jamais en production).
    /// </summary>
    public static async Task InitializeAsync(WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:AutoMigrate"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    }
}
