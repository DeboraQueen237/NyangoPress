namespace NyangoPress.Domain.Common;

/// <summary>
/// Represents a domain event emitted during business operations.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
