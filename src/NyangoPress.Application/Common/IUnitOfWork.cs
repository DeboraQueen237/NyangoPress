namespace NyangoPress.Application.Common;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ISequenceGenerator
{
    Task<string> NextTicketNumberAsync(CancellationToken ct = default);
    Task<string> NextCustomerCodeAsync(CancellationToken ct = default);
    Task<string> NextTourCodeAsync(CancellationToken ct = default);
    Task<string> NextPickupReferenceAsync(CancellationToken ct = default);
    Task<string> NextAuctionLotNumberAsync(CancellationToken ct = default);
}

public interface IRealtimeNotifier
{
    Task NotifyOrderUpdatedAsync(Guid orderId, string ticketNumber, CancellationToken ct = default);
    Task NotifyProductionItemChangedAsync(Guid orderId, Guid itemId, string newStatus, CancellationToken ct = default);
    Task NotifyDriverLocationAsync(
        Guid driverId,
        string driverName,
        Guid? deliveryOrderId,
        string? ticketNumber,
        double latitude,
        double longitude,
        DateTimeOffset recordedAt,
        CancellationToken ct = default);
    Task NotifyDeliveryStatusChangedAsync(Guid deliveryOrderId, string ticketNumber, string status, CancellationToken ct = default);
}
