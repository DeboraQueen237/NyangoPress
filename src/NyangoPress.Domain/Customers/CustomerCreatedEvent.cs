using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Customers;

public sealed record CustomerCreatedEvent(
    Guid CustomerId,
    string CustomerCode,
    string FullName,
    string Phone) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
