using NyangoPress.Domain.Common;
using NyangoPress.Domain.Orders;

namespace NyangoPress.Domain.Delivery;

public class DeliveryOrder : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ZoneId { get; set; }
    public DeliveryZone? Zone { get; set; }

    public decimal DeliveryFeeFcfa { get; set; }
    public DeliveryMissionType MissionType { get; set; } = DeliveryMissionType.Delivery;
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

    public Guid? TourId { get; set; }
    public DeliveryTour? Tour { get; set; }
    public int StopSequence { get; set; }

    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }

    public string Address { get; set; } = string.Empty;
    public double? DestinationLatitude { get; set; }
    public double? DestinationLongitude { get; set; }
    public string? TimeSlot { get; set; }

    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? ArrivedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public string OtpCode { get; set; } = string.Empty;
    public string? ProofPhotoPath { get; set; }
    public string? ProofPhotoUrl { get; set; }
    public string? ProofSignaturePath { get; set; }
    public string? ProofSignatureUrl { get; set; }
    public string? FailureReason { get; set; }
}
