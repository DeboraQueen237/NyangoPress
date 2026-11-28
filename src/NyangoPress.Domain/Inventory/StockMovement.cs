using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Inventory;

public enum StockMovementType
{
    Entry = 0,             // entrée manuelle
    Consumption = 1,       // sortie (utilisation en production)
    Adjustment = 2,        // correction manuelle
    InventoryCount = 3,    // écart constaté lors d'un inventaire
    PurchaseReceipt = 4,   // réception d'un bon de commande
    Loss = 5               // casse, péremption
}

/// <summary>Historique immuable : on ne modifie ni ne supprime un mouvement, on en ajoute un autre.</summary>
public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public StockMovementType Type { get; set; }
    /// <summary>Signé : positif = entrée, négatif = sortie.</summary>
    public decimal Quantity { get; set; }
    /// <summary>Stock après le mouvement (permet de reconstituer l'historique).</summary>
    public decimal QuantityAfter { get; set; }

    public Guid PerformedByUserId { get; set; }
    public string? Reason { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
