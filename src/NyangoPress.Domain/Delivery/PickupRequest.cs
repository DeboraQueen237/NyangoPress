using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Delivery;

public class PickupRequest : BaseEntity
{
    public string ReferenceCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public Guid ZoneId { get; set; }
    public DeliveryZone? Zone { get; set; }
    public string Address { get; set; } = string.Empty;
    public string PreferredTimeSlot { get; set; } = string.Empty;
    public int EstimatedItemsCount { get; set; }
    public string? Notes { get; set; }
    public PickupRequestStatus Status { get; set; } = PickupRequestStatus.Pending;
    public string? ReviewNotes { get; set; }
    public Guid? LinkedOrderId { get; set; }
}
