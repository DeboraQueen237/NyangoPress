using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NyangoPress.Domain.Identity;
using NyangoPress.Infrastructure.Persistence;

namespace NyangoPress.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Configure ASP.NET Core Identity : politique de mot de passe (12 caractères, majuscule, minuscule, chiffre,
    /// mots de passe courants refusés), verrouillage après 5 échecs pendant 15 minutes, hachage PBKDF2.
    /// Utilisé par l'API ET par les tests : une seule configuration.
    /// </summary>
    public static IServiceCollection AddNyangoIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 4;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<CommonPasswordValidator>();

        // PBKDF2-HMAC-SHA512 avec sel aléatoire par mot de passe ; 210 000 itérations (recommandation OWASP).
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);

        return services;
    }
}
