using NyangoPress.Domain.Suppliers;

namespace NyangoPress.Application.Suppliers;

/// <summary>CONTRAT PARTAGÉ (propriétaire : Dev 5 — utilisé par Dev 3 pour les dépenses et rapports, Dev 4 pour l'UI).</summary>
public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrderDto>> GetAsync(PurchaseOrderStatus? status = null, CancellationToken ct = default);
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseOrderDto> CreateDraftAsync(CreatePurchaseOrderCommand command, CancellationToken ct = default);

    /// <summary>Génère le PDF (IDocumentService) et l'envoie au fournisseur (INotificationService).</summary>
    Task SendToSupplierAsync(Guid purchaseOrderId, CancellationToken ct = default);

    /// <summary>Réception totale ou partielle : met à jour le stock via des mouvements PurchaseReceipt.</summary>
    Task<PurchaseOrderDto> ReceiveAsync(ReceivePurchaseOrderCommand command, CancellationToken ct = default);

    Task CancelAsync(Guid purchaseOrderId, string reason, CancellationToken ct = default);
}

public sealed record PurchaseOrderDto(
    Guid Id,
    string Number,
    Guid SupplierId,
    string SupplierName,
    PurchaseOrderStatus Status,
    DateOnly? ExpectedDate,
    decimal Total,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record PurchaseOrderLineDto(Guid ProductId, string ProductName, decimal QuantityOrdered, decimal QuantityReceived, decimal UnitPrice);

public sealed record CreatePurchaseOrderCommand(Guid SupplierId, DateOnly? ExpectedDate, string? Notes, IReadOnlyList<PurchaseOrderLineInput> Lines);
public sealed record PurchaseOrderLineInput(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record ReceivePurchaseOrderCommand(Guid PurchaseOrderId, IReadOnlyList<ReceivedLineInput> Lines);
public sealed record ReceivedLineInput(Guid ProductId, decimal QuantityReceived);
