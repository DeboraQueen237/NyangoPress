using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Orders;

public class ItemPhoto : BaseEntity
{
    public Guid OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public long SizeBytes { get; set; }
    public string? Caption { get; set; }
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
}
