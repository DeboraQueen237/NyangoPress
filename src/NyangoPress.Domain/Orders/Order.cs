using NyangoPress.Domain.Common;
using NyangoPress.Domain.Customers;
using NyangoPress.Domain.Delivery;

namespace NyangoPress.Domain.Orders;

public class Order : BaseEntity
{
    public string TicketNumber { get; set; } = string.Empty;
    public string TrackingCode { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Received;
    public DeliveryOption DeliveryOption { get; set; } = DeliveryOption.StorePickup;
    public Guid? DeliveryZoneId { get; set; }
    public string? DeliveryAddress { get; set; }
    public bool IsExpress { get; set; }

    public bool AcceptedTermsAndConditions { get; set; }
    public DateTimeOffset? TermsAcceptedAt { get; set; }
    public string? TermsSummary { get; set; }

    public decimal SubtotalFcfa { get; set; }
    public decimal DeliveryFeeFcfa { get; set; }
    public decimal StorageFeeFcfa { get; set; }
    public decimal DiscountFcfa { get; set; }
    public decimal TotalFcfa { get; set; }
    public decimal PaidFcfa { get; set; }

    public decimal BalanceFcfa => Math.Max(0m, TotalFcfa + StorageFeeFcfa - PaidFcfa);

    public DateTimeOffset PromisedAt { get; set; }
    public DateTimeOffset EstimatedReadyAt { get; set; }
    public DateTimeOffset? ReadyAt { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public string? PickupSignaturePath { get; set; }

    public bool BalanceOverrideUsed { get; set; }
    public string? BalanceOverrideReason { get; set; }
    public string? BalanceOverrideBy { get; set; }

    public string? PromoCode { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    public DeliveryOrder? DeliveryOrder { get; set; }

    public void RefreshStatus(DateTimeOffset now)
    {
        Status = ItemStateMachine.ComputeOrderStatus(Items.Select(i => i.Status).ToList());
        if (Status == OrderStatus.Ready && ReadyAt is null)
        {
            ReadyAt = now;
        }
        else if (Status == OrderStatus.Delivered && PickedUpAt is null)
        {
            PickedUpAt = now;
        }
    }
}
