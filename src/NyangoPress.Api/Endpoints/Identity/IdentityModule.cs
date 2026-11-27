using FluentValidation;
using NyangoPress.Api.Modules;
using NyangoPress.Application.Identity;
using NyangoPress.Infrastructure.Identity;
using NyangoPress.Infrastructure.Seed;

namespace NyangoPress.Api.Endpoints.Identity;

/// <summary>Authentification : connexion, jetons, mot de passe.</summary>
public sealed class IdentityModule : IModule
{
    public int Order => 10;

    public void AddServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddDataProtection();
        services.AddNyangoIdentity();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<ISeeder, IdentitySeeder>();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
    }

    public void MapEndpoints(WebApplication app) => app.MapAuthEndpoints();
}
