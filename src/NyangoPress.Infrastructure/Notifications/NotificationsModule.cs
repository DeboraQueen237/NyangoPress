using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NyangoPress.Application.Inventory;
using NyangoPress.Application.Notifications;
using NyangoPress.Infrastructure.Inventory;

namespace NyangoPress.Infrastructure.Notifications;

/// <summary>
/// Extensions de module (règle 4.2) : Dev 1 ajoute UNE fois dans Program.cs :
///     builder.Services.AddNotificationsModule(builder.Configuration);
///     builder.Services.AddInventoryModule(builder.Configuration);
/// Ensuite, Dev 5 ne touche plus Program.cs : tout se branche ici.
/// </summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // J1 : fake par défaut. J2 : passer Notifications:UseFake à false quand l'outbox est prête.
        var useFake = configuration.GetValue("Notifications:UseFake", defaultValue: true);

        if (useFake)
        {
            services.AddSingleton<FakeNotificationService>();
            services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<FakeNotificationService>());
        }
        else
        {
            // J2 : services.AddScoped<INotificationService, NotificationService>();
            //      services.AddScoped<ITemplateRenderer, TemplateRenderer>();
            //      services.AddScoped<INotificationChannelSender, EmailChannel>(); ...
            //      Hangfire (stockage PostgreSQL) + OutboxProcessor récurrent
            throw new NotSupportedException("Implémentation réelle des notifications prévue en J2.");
        }

        return services;
    }

    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        var useFake = configuration.GetValue("Inventory:UseFake", defaultValue: true);
        if (useFake)
            services.AddSingleton<IStockService, FakeStockService>();
        else
            throw new NotSupportedException("Implémentation réelle des stocks prévue en J3.");

        // IPurchaseOrderService : fake ajouté si Dev 3/Dev 4 en ont besoin avant J4.
        return services;
    }
}
