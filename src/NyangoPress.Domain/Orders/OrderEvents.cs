using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Orders;

public sealed record OrderCreatedEvent(
    Guid OrderId,
    string TicketNumber,
    Guid CustomerId,
    decimal TotalFcfa,
    DateTimeOffset PromisedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record OrderStatusChangedEvent(
    Guid OrderId,
    string TicketNumber,
    Guid CustomerId,
    OrderStatus PreviousStatus,
    OrderStatus NewStatus,
    Guid? OrderItemId = null,
    ItemStatus? ItemStatus = null) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record OrderCancelledEvent(
    Guid OrderId,
    string TicketNumber,
    Guid CustomerId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record OrderPickedUpEvent(
    Guid OrderId,
    string TicketNumber,
    Guid CustomerId,
    decimal FinalPaymentFcfa,
    bool BalanceOverrideUsed) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ClaimOpenedEvent(
    Guid ClaimId,
    Guid OrderId,
    string TicketNumber,
    string Title) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
