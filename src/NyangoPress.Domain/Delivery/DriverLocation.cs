using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Delivery;

public class DriverLocation : BaseEntity
{
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public Guid? DeliveryOrderId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
