using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NyangoPress.Application.Common;
using NyangoPress.Application.Identity;

namespace NyangoPress.Api.Modules;

/// <summary>Authentification JWT, politiques d'autorisation, limitation de débit et CORS.</summary>
public sealed class SecurityModule : IModule
{
    public int Order => 1;

    public void AddServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        {
            if (builder.Environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "Jwt:Key est manquante ou trop courte (32 caractères minimum). Définissez la variable d'environnement Jwt__Key.");
            }

            // Développement / tests : clé aléatoire à chaque démarrage (les sessions n'y survivent pas, c'est voulu).
            jwt.Key = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(48));
        }

        services.AddSingleton(Options.Create(jwt));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Les noms de claims restent ceux du jeton ("sub", "role"...).
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
            });

        services.AddAuthorization(options =>
        {
            foreach (var (policyName, roles) in Policies.RolesByPolicy)
            {
                options.AddPolicy(policyName, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(roles);

                    if (policyName != Policies.AuthenticatedAny)
                    {
                        // Mot de passe temporaire pas encore changé : tout est refusé sauf changement de mot de passe / déconnexion.
                        policy.RequireAssertion(ctx => !ctx.User.HasClaim("must_change_pwd", "true"));
                    }
                });
            }

            // Tout endpoint est protégé par défaut. Pour un endpoint public : .AllowAnonymous() (à justifier en revue).
            options.FallbackPolicy = options.GetPolicy(Policies.Authenticated);
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", ctx => Limit(ctx, 10));
            options.AddPolicy("otp", ctx => Limit(ctx, 5));
            options.AddPolicy("public", ctx => Limit(ctx, 30));
        });

        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (origins.Length > 0)
            {
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
            }
        }));
    }

    public void MapEndpoints(WebApplication app)
    {
    }

    // Une fenêtre fixe d'une minute par adresse IP.
    private static RateLimitPartition<string> Limit(HttpContext context, int permitPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
}
