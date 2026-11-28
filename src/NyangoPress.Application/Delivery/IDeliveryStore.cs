using NyangoPress.Domain.Delivery;
using NyangoPress.Domain.Orders;
using SharedDeliveryStatus = NyangoPress.Shared.Delivery.DeliveryStatus;
using SharedPickupRequestStatus = NyangoPress.Shared.Delivery.PickupRequestStatus;

namespace NyangoPress.Application.Delivery;

public interface IDeliveryStore
{
    Task<IReadOnlyList<DeliveryZone>> GetZonesAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<DeliveryZone?> GetZoneByIdAsync(Guid id, CancellationToken ct = default);
    Task AddZoneAsync(DeliveryZone zone, CancellationToken ct = default);

    Task<IReadOnlyList<DeliveryOrder>> GetDeliveriesAsync(SharedDeliveryStatus? status = null, Guid? driverId = null, CancellationToken ct = default);
    Task<DeliveryOrder?> GetDeliveryByIdAsync(Guid id, CancellationToken ct = default);
    Task<DeliveryOrder?> GetDeliveryByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    Task<IReadOnlyList<DeliveryTour>> GetToursAsync(DateOnly? date = null, Guid? driverId = null, CancellationToken ct = default);
    Task<DeliveryTour?> GetTourByIdAsync(Guid id, CancellationToken ct = default);
    Task AddTourAsync(DeliveryTour tour, CancellationToken ct = default);

    Task<DriverLocation?> GetLatestDriverLocationAsync(Guid driverId, CancellationToken ct = default);
    Task AddDriverLocationAsync(DriverLocation location, CancellationToken ct = default);

    Task<IReadOnlyList<PickupRequest>> GetPickupRequestsAsync(SharedPickupRequestStatus? status = null, CancellationToken ct = default);
    Task<PickupRequest?> GetPickupRequestByIdAsync(Guid id, CancellationToken ct = default);
    Task AddPickupRequestAsync(PickupRequest request, CancellationToken ct = default);

    Task<Order?> GetOrderByTicketAndCodeAsync(string ticketNumber, string trackingCode, CancellationToken ct = default);
}
