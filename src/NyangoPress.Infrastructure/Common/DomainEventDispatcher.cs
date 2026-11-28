using Microsoft.Extensions.Logging;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Common;

namespace NyangoPress.Infrastructure.Common;

public sealed class DomainEventDispatcher(ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        logger.LogInformation("Domain event dispatched: {EventType} ({EventId}) at {OccurredAt}",
            domainEvent.GetType().Name, domainEvent.EventId, domainEvent.OccurredAt);
        return Task.CompletedTask;
    }
}

public sealed class LoggingNotificationService(ILogger<LoggingNotificationService> logger) : INotificationService
{
    public Task EnqueueAsync(NotificationRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Notification enqueued: template={TemplateKey}, customerId={CustomerId}, phone={Phone}, email={Email}",
            request.TemplateKey, request.CustomerId, request.Phone, request.Email);
        return Task.CompletedTask;
    }
}
