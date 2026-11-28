using NyangoPress.Application.Common;
using NyangoPress.Shared.Delivery;
using NyangoPress.Shared.Tracking;

namespace NyangoPress.Application.Delivery;

public interface IDeliveryService
{
    Task<IReadOnlyList<DeliveryZoneDto>> GetZonesAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<Result<DeliveryZoneDto>> CreateZoneAsync(UpsertDeliveryZoneRequest request, CancellationToken ct = default);
    Task<Result<DeliveryZoneDto>> UpdateZoneAsync(Guid id, UpsertDeliveryZoneRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<DeliveryOrderDto>> GetDeliveriesAsync(DeliveryStatus? status = null, Guid? driverId = null, CancellationToken ct = default);
    Task<Result<DeliveryOrderDto>> AssignDriverAsync(Guid deliveryId, AssignDeliveryRequest request, CancellationToken ct = default);
    Task<Result<DeliveryOrderDto>> UpdateStatusAsync(Guid deliveryId, UpdateDeliveryStatusRequest request, CancellationToken ct = default);
    Task<Result<DeliveryOrderDto>> CompleteDeliveryAsync(Guid deliveryId, CompleteDeliveryRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<DeliveryTourDto>> GetToursAsync(DateOnly? date = null, Guid? driverId = null, CancellationToken ct = default);
    Task<Result<DeliveryTourDto>> CreateTourAsync(CreateTourRequest request, CancellationToken ct = default);
    Task<Result<DeliveryTourDto>> ReorderTourStopsAsync(Guid tourId, ReorderTourStopsRequest request, CancellationToken ct = default);
    Task<Result<DeliveryTourDto>> StartTourAsync(Guid tourId, CancellationToken ct = default);

    Task<Result<DriverLocationDto>> BroadcastLocationAsync(BroadcastLocationRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PickupRequestDto>> GetPickupRequestsAsync(PickupRequestStatus? status = null, CancellationToken ct = default);
    Task<Result<PickupRequestDto>> ReviewPickupRequestAsync(Guid id, ReviewPickupRequest request, CancellationToken ct = default);
}

public interface IPublicTrackingService
{
    Task<Result<PublicOrderTrackingDto>> TrackOrderAsync(string ticketNumber, string trackingCode, CancellationToken ct = default);
    Task<Result<PublicPickupRequestResultDto>> SubmitPickupRequestAsync(CreatePickupRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DeliveryZoneDto>> GetPublicZonesAsync(CancellationToken ct = default);
}
