namespace NyangoPress.Shared.Delivery;

public enum DeliveryMissionType
{
    Pickup = 0,
    Delivery = 1
}

public enum DeliveryStatus
{
    Pending = 0,
    Assigned = 1,
    EnRoute = 2,
    Arrived = 3,
    NearDestination = 4,
    Delivered = 5,
    Failed = 6,
    Cancelled = 7
}

public enum PickupRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    ConvertedToOrder = 3
}

public sealed record DeliveryZoneDto(
    Guid Id,
    string Name,
    string Districts,
    decimal FeeFcfa,
    string TimeSlots,
    int MinimumDelayHours,
    bool IsActive,
    double CenterLatitude,
    double CenterLongitude,
    long RowVersion);

public sealed record DriverLocationDto(
    Guid DriverId,
    string DriverName,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    DateTimeOffset RecordedAt);

public sealed record DeliveryOrderDto(
    Guid Id,
    Guid OrderId,
    string TicketNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    Guid ZoneId,
    string ZoneName,
    decimal DeliveryFeeFcfa,
    DeliveryMissionType MissionType,
    DeliveryStatus Status,
    Guid? TourId,
    int StopSequence,
    Guid? DriverId,
    string? DriverName,
    string Address,
    double? DestinationLatitude,
    double? DestinationLongitude,
    string? TimeSlot,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? AssignedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ArrivedAt,
    DateTimeOffset? CompletedAt,
    string? OtpCodeForStaffPreview,
    string? ProofPhotoUrl,
    string? ProofSignatureUrl,
    string? FailureReason,
    DriverLocationDto? LatestDriverLocation,
    long RowVersion);

public sealed record DeliveryTourDto(
    Guid Id,
    string TourCode,
    Guid DriverId,
    string DriverName,
    DateOnly TourDate,
    bool IsStarted,
    bool IsCompleted,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<DeliveryOrderDto> Stops);

public sealed record PickupRequestDto(
    Guid Id,
    string ReferenceCode,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    Guid ZoneId,
    string ZoneName,
    string Address,
    string PreferredTimeSlot,
    int EstimatedItemsCount,
    string? Notes,
    PickupRequestStatus Status,
    string? ReviewNotes,
    Guid? LinkedOrderId,
    DateTimeOffset CreatedAt);

public sealed record UpsertDeliveryZoneRequest(
    string Name,
    string Districts,
    decimal FeeFcfa,
    string TimeSlots,
    int MinimumDelayHours,
    bool IsActive,
    double CenterLatitude = 4.0511,
    double CenterLongitude = 9.7679);

public sealed record AssignDeliveryRequest(
    Guid DriverId,
    string DriverName,
    Guid? TourId = null,
    string? TimeSlot = null);

public sealed record CreateTourRequest(
    Guid DriverId,
    string DriverName,
    DateOnly TourDate,
    IReadOnlyList<Guid> DeliveryOrderIds);

public sealed record ReorderTourStopsRequest(
    IReadOnlyList<Guid> OrderedDeliveryIds);

public sealed record UpdateDeliveryStatusRequest(
    DeliveryStatus Status,
    string? Notes = null);

public sealed record CompleteDeliveryRequest(
    string? OtpCode,
    string? ProofPhotoDataUrl,
    string? ProofSignatureDataUrl);

public sealed record BroadcastLocationRequest(
    Guid? DeliveryOrderId,
    double Latitude,
    double Longitude,
    double? AccuracyMeters = null);

public sealed record CreatePickupRequest(
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    Guid ZoneId,
    string Address,
    string PreferredTimeSlot,
    int EstimatedItemsCount,
    string? Notes);

public sealed record ReviewPickupRequest(
    bool Approve,
    string? ReviewNotes,
    Guid? AssignDriverId = null,
    string? AssignDriverName = null);
