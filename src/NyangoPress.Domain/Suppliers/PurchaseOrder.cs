using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Suppliers;

public enum PurchaseOrderStatus
{
    Draft = 0,
    Sent = 1,
    PartiallyReceived = 2,
    Received = 3,
    Cancelled = 4
}

/// <summary>Bon de commande fournisseur (SUP). Le PDF est généré via IDocumentService (Dev 3).</summary>
public class PurchaseOrder : BaseEntity
{
    /// <summary>Numéro lisible, ex. "BC-2026-0001".</summary>
    public string Number { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public DateTimeOffset? SentAt { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string? Notes { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = new();

    public decimal Total => Lines.Sum(l => l.QuantityOrdered * l.UnitPrice);
    public bool IsFullyReceived => Lines.Count > 0 && Lines.All(l => l.QuantityReceived >= l.QuantityOrdered);
}

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitPrice { get; set; }
}
