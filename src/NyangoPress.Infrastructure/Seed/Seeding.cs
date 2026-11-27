using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NyangoPress.Domain.Common;
using NyangoPress.Domain.Identity;
using NyangoPress.Infrastructure.Persistence;

namespace NyangoPress.Infrastructure.Seed;

/// <summary>
/// Étape de remplissage initial de la base. Pour ajouter vos données de départ (catégories, modèles de message...) :
/// créez une classe qui implémente ISeeder dans votre module et enregistrez-la dans votre classe Module
/// (<c>services.AddScoped&lt;ISeeder, MonSeeder&gt;()</c>). Un seeder doit pouvoir être exécuté plusieurs fois sans doublon.
/// </summary>
public interface ISeeder
{
    /// <summary>Ordre d'exécution (0 = succursale, 10 = rôles et administrateur, 100 = par défaut).</summary>
    int Order { get; }

    Task SeedAsync(CancellationToken ct);
}

public sealed class DatabaseSeeder(IEnumerable<ISeeder> seeders, ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        foreach (var seeder in seeders.OrderBy(s => s.Order))
        {
            logger.LogInformation("Seed : {Seeder}", seeder.GetType().Name);
            await seeder.SeedAsync(ct);
        }
    }
}

public sealed class SeedOptions
{
    public string AdminUserName { get; set; } = "admin";

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;

    public string AdminFullName { get; set; } = "Administrateur";
}

public sealed class BranchSeeder(AppDbContext db) : ISeeder
{
    public int Order => 0;

    public async Task SeedAsync(CancellationToken ct)
    {
        if (await db.Set<Branch>().AnyAsync(b => b.Id == DefaultBranch.Id, ct))
        {
            return;
        }

        db.Set<Branch>().Add(new Branch { Id = DefaultBranch.Id, Code = DefaultBranch.Code, Name = DefaultBranch.Name });
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Crée les 6 rôles et, si les secrets sont renseignés, le premier compte administrateur.</summary>
public sealed class IdentitySeeder(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    AppDbContext db,
    IOptions<SeedOptions> options,
    ILogger<IdentitySeeder> logger) : ISeeder
{
    public int Order => 10;

    public async Task SeedAsync(CancellationToken ct)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole(role));
            }
        }

        if ((await userManager.GetUsersInRoleAsync(Roles.Admin)).Count > 0)
        {
            return;
        }

        var seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogWarning(
                "Aucun administrateur créé. Renseignez Seed:AdminEmail et Seed:AdminPassword avec 'dotnet user-secrets' puis relancez.");
            return;
        }

        var admin = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = seed.AdminUserName,
            Email = seed.AdminEmail,
            EmailConfirmed = true,
            FullName = seed.AdminFullName,
            IsActive = true,
            MustChangePassword = false,
            BranchId = DefaultBranch.Id,
        };

        var created = await userManager.CreateAsync(admin, seed.AdminPassword);
        if (!created.Succeeded)
        {
            logger.LogError(
                "Création de l'administrateur refusée : {Errors}",
                string.Join(" | ", created.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);
        db.Set<EmployeeProfile>().Add(new EmployeeProfile { UserId = admin.Id, BranchId = DefaultBranch.Id });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Administrateur '{UserName}' créé.", seed.AdminUserName);
    }
}
