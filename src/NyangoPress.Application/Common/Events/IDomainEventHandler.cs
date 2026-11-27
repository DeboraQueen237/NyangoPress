namespace NyangoPress.Application.Common.Events;

/// <summary>
/// Réagit à un événement. Enregistrement : <c>services.AddScoped&lt;IDomainEventHandler&lt;OrderCreated&gt;, MonHandler&gt;()</c>
/// dans la classe Module du dossier concerné.
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct);
}
