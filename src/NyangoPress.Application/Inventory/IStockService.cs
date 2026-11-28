using NyangoPress.Domain.Inventory;

namespace NyangoPress.Application.Inventory;

/// <summary>
/// CONTRAT PARTAGÉ (propriétaire : Dev 5 — utilisé par Dev 3 pour les rapports, Dev 4 pour les tableaux de bord).
/// </summary>
public interface IStockService
{
    Task<IReadOnlyList<ProductStockDto>> GetProductsAsync(bool onlyBelowThreshold = false, CancellationToken ct = default);
    Task<ProductStockDto?> GetProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Enregistre un mouvement. Refuse un stock négatif. L'auteur est l'utilisateur courant (ICurrentUser).
    /// Publie StockBelowThreshold si le seuil est franchi.
    /// </summary>
    Task<StockMovementDto> RecordMovementAsync(RecordStockMovementCommand command, CancellationToken ct = default);

    Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid productId, DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default);

    /// <summary>Valeur totale du stock (somme quantité × coût unitaire) — pour les rapports de Dev 3.</summary>
    Task<decimal> GetStockValueAsync(CancellationToken ct = default);
}

public sealed record ProductStockDto(
    Guid Id,
    string Name,
    string Sku,
    string Unit,
    decimal QuantityOnHand,
    decimal MinThreshold,
    decimal UnitCost,
    bool IsBelowThreshold,
    bool IsOutOfStock);

public sealed record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    StockMovementType Type,
    decimal Quantity,
    decimal QuantityAfter,
    Guid PerformedByUserId,
    string? Reason,
    DateTimeOffset OccurredAt);

/// <param name="Quantity">Positif = entrée, négatif = sortie.</param>
public sealed record RecordStockMovementCommand(Guid ProductId, StockMovementType Type, decimal Quantity, string? Reason);
