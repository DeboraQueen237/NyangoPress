using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Orders;

public class Claim : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid? OrderItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; } = ClaimStatus.Open;
    public string? PhotoStoragePath { get; set; }
    public string? PhotoPublicUrl { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? GoodwillGestureFcfa { get; set; }
    public string OpenedBy { get; set; } = string.Empty;
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ResolvedBy { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
