namespace NyangoPress.Application.Common.Events;

/// <summary>Événement métier publié dans l'application pour que les modules communiquent sans dépendance directe.</summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}

/// <summary>Classe de base des événements : identifiant et horodatage automatiques.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
