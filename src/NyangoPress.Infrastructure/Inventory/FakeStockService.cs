using NyangoPress.Application.Inventory;
using NyangoPress.Domain.Inventory;

namespace NyangoPress.Infrastructure.Inventory;

/// <summary>Données d'exemple pour Dev 3 (rapports) et Dev 4 (tableaux de bord) en attendant l'implémentation réelle (J3).</summary>
public sealed class FakeStockService : IStockService
{
    private static readonly List<ProductStockDto> Products =
    [
        new(Guid.Parse("11111111-0000-0000-0000-000000000001"), "Lessive liquide professionnelle", "LES-001", "L", 42m, 10m, 2500m, false, false),
        new(Guid.Parse("11111111-0000-0000-0000-000000000002"), "Détachant universel", "DET-001", "L", 3m, 5m, 4000m, true, false),
        new(Guid.Parse("11111111-0000-0000-0000-000000000003"), "Cintres métalliques", "CIN-001", "pièce", 0m, 100m, 50m, true, true),
        new(Guid.Parse("11111111-0000-0000-0000-000000000004"), "Housses plastiques", "HOU-001", "pièce", 350m, 200m, 25m, false, false)
    ];

    public Task<IReadOnlyList<ProductStockDto>> GetProductsAsync(bool onlyBelowThreshold = false, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ProductStockDto>>(
            onlyBelowThreshold ? Products.Where(p => p.IsBelowThreshold).ToList() : Products);

    public Task<ProductStockDto?> GetProductAsync(Guid productId, CancellationToken ct = default)
        => Task.FromResult(Products.FirstOrDefault(p => p.Id == productId));

    public Task<StockMovementDto> RecordMovementAsync(RecordStockMovementCommand command, CancellationToken ct = default)
    {
        var p = Products.FirstOrDefault(x => x.Id == command.ProductId)
                ?? throw new KeyNotFoundException("Produit introuvable.");
        return Task.FromResult(new StockMovementDto(Guid.NewGuid(), p.Id, p.Name, command.Type, command.Quantity,
            p.QuantityOnHand + command.Quantity, Guid.Empty, command.Reason, DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid productId, DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<StockMovementDto>>([]);

    public Task<decimal> GetStockValueAsync(CancellationToken ct = default)
        => Task.FromResult(Products.Sum(p => p.QuantityOnHand * p.UnitCost));
}
