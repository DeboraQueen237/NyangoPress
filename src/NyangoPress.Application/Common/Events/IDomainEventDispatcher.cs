namespace NyangoPress.Application.Common.Events;

public interface IDomainEventDispatcher
{
    /// <summary>
    /// Appelle tous les handlers enregistrés pour cet événement. Une erreur dans un handler est journalisée
    /// mais ne casse ni l'opération principale ni les autres handlers.
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
