using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Delivery;

public sealed record DeliveryStatusChangedEvent(
    Guid DeliveryOrderId,
    Guid OrderId,
    Guid CustomerId,
    string TicketNumber,
    DeliveryStatus Status,
    string? OtpCode) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
