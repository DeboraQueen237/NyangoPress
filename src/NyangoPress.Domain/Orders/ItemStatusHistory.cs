using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Orders;

public class ItemStatusHistory : BaseEntity
{
    public Guid OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    public ItemStatus? FromStatus { get; set; }
    public ItemStatus ToStatus { get; set; }
    public TreatmentSubStep SubStep { get; set; } = TreatmentSubStep.None;
    public string ChangedBy { get; set; } = "System";
    public Guid? ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
}
