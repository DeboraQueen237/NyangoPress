using NyangoPress.Shared.Delivery;
using NyangoPress.Shared.Orders;

namespace NyangoPress.Shared.Tracking;

public sealed record PublicTrackingItemDto(
    string Label,
    string ServiceName,
    ItemStatus Status,
    bool IsExpress,
    string? StorageLocationCode,
    IReadOnlyList<PublicTimelineStepDto> Timeline);

public sealed record PublicTimelineStepDto(
    ItemStatus Status,
    DateTimeOffset ChangedAt);

public sealed record PublicDeliveryTrackingDto(
    DeliveryStatus Status,
    string ZoneName,
    string? TimeSlot,
    string? DriverFirstName,
    double? DriverLatitude,
    double? DriverLongitude,
    double? DestinationLatitude,
    double? DestinationLongitude,
    DateTimeOffset? LastLocationAt);

public sealed record PublicOrderTrackingDto(
    string TicketNumber,
    string CustomerMaskedName,
    OrderStatus Status,
    DeliveryOption DeliveryOption,
    bool IsExpress,
    DateTimeOffset CreatedAt,
    DateTimeOffset PromisedAt,
    DateTimeOffset EstimatedReadyAt,
    int ItemsCount,
    int ReadyItemsCount,
    decimal TotalFcfa,
    decimal PaidFcfa,
    decimal RemainingBalanceFcfa,
    IReadOnlyList<PublicTrackingItemDto> Items,
    PublicDeliveryTrackingDto? Delivery);

public sealed record PublicPickupRequestResultDto(
    Guid Id,
    string ReferenceCode,
    PickupRequestStatus Status,
    string ZoneName,
    string PreferredTimeSlot,
    DateTimeOffset CreatedAt);
