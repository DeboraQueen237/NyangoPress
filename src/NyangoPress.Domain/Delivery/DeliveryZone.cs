using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Delivery;

public class DeliveryZone : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Districts { get; set; } = string.Empty;
    public decimal FeeFcfa { get; set; }
    public string TimeSlots { get; set; } = "09:00-12:00, 14:00-18:00";
    public int MinimumDelayHours { get; set; } = 4;
    public bool IsActive { get; set; } = true;
    public double CenterLatitude { get; set; } = 4.0511;
    public double CenterLongitude { get; set; } = 9.7679;
}
