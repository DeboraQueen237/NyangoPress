using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Inventory;

/// <summary>Consommable du pressing (lessive, détachant, cintres, housses...).</summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Sku { get; set; } = default!;
    /// <summary>Unité de stock : "L", "kg", "pièce"...</summary>
    public string Unit { get; set; } = "pièce";

    public decimal QuantityOnHand { get; private set; }
    /// <summary>Seuil d'alerte « stock bas ».</summary>
    public decimal MinThreshold { get; set; }
    /// <summary>Quantité suggérée lors d'un réapprovisionnement.</summary>
    public decimal ReorderQuantity { get; set; }
    public decimal UnitCost { get; set; }

    public Guid? DefaultSupplierId { get; set; }
    public bool IsActive { get; set; } = true;

    public List<StockMovement> Movements { get; set; } = new();

    public bool IsBelowThreshold => QuantityOnHand <= MinThreshold;
    public bool IsOutOfStock => QuantityOnHand <= 0;

    /// <summary>
    /// Seul point d'entrée pour changer le stock. Règles :
    /// le stock ne devient jamais négatif, chaque mouvement a un auteur.
    /// </summary>
    /// <param name="delta">Positif = entrée, négatif = sortie.</param>
    public StockMovement ApplyMovement(StockMovementType type, decimal delta, Guid performedByUserId,
        string? reason = null, Guid? purchaseOrderId = null)
    {
        if (performedByUserId == Guid.Empty)
            throw new InvalidOperationException("Un mouvement de stock doit avoir un auteur.");
        if (delta == 0)
            throw new ArgumentException("La quantité du mouvement ne peut pas être nulle.", nameof(delta));
        if (QuantityOnHand + delta < 0)
            throw new InvalidOperationException(
                $"Stock insuffisant pour {Name} : disponible {QuantityOnHand} {Unit}, demandé {-delta} {Unit}.");

        QuantityOnHand += delta;

        var movement = new StockMovement
        {
            ProductId = Id,
            Type = type,
            Quantity = delta,
            QuantityAfter = QuantityOnHand,
            PerformedByUserId = performedByUserId,
            Reason = reason,
            PurchaseOrderId = purchaseOrderId,
            OccurredAt = DateTimeOffset.UtcNow
        };
        Movements.Add(movement);
        return movement;
    }
}
