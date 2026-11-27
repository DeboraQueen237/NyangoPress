using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NyangoPress.Api.Common;
using NyangoPress.Application.Common;
using NyangoPress.Application.Common.Events;
using NyangoPress.Infrastructure.Audit;
using NyangoPress.Infrastructure.Common;
using NyangoPress.Infrastructure.Persistence;
using NyangoPress.Infrastructure.Seed;
using NyangoPress.Infrastructure.Storage;
using Scalar.AspNetCore;

namespace NyangoPress.Api.Modules;

/// <summary>Noyau : base de données, audit, utilisateur courant, stockage, événements, santé, documentation de l'API.</summary>
public sealed class CoreModule : IModule
{
    public int Order => 0;

    public void AddServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:Default est manquante. Voir README : dotnet user-secrets set \"ConnectionStrings:Default\" \"...\"");

            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAuditService, AuditService>();

        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.Configure<SeedOptions>(configuration.GetSection("Seed"));
        services.AddScoped<ISeeder, BranchSeeder>();
        services.AddScoped<DatabaseSeeder>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
        services.AddOpenApi();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    }

    public void MapEndpoints(WebApplication app)
    {
        // Sonde de vie (le processus répond) et sonde de disponibilité (la base répond).
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready").AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference().AllowAnonymous();
        }
    }
}
