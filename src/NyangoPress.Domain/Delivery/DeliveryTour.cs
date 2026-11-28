using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Delivery;

public class DeliveryTour : BaseEntity
{
    public string TourCode { get; set; } = string.Empty;
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public DateOnly TourDate { get; set; }
    public bool IsStarted { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<DeliveryOrder> Stops { get; set; } = new List<DeliveryOrder>();
}
