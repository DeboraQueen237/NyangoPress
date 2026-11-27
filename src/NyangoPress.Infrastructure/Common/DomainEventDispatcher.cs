using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NyangoPress.Application.Common.Events;

namespace NyangoPress.Infrastructure.Common;

/// <summary>
/// Dispatcher d'événements en mémoire : retrouve les handlers enregistrés dans le conteneur d'injection
/// (IDomainEventHandler&lt;TypeDeLEvenement&gt;) et les appelle l'un après l'autre.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider services, ILogger<DomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

        foreach (var handler in services.GetServices(handlerType))
        {
            try
            {
                var task = (Task)method.Invoke(handler, [domainEvent, ct])!;
                await task;
            }
            catch (Exception ex)
            {
                // Un handler défaillant ne doit pas casser l'opération principale ni les autres handlers.
                logger.LogError(ex, "Le handler {Handler} a échoué pour l'événement {Event}", handler?.GetType().Name, eventType.Name);
            }
        }
    }
}
