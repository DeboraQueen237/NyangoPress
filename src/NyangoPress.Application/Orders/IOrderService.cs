using NyangoPress.Application.Common;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Orders;

namespace NyangoPress.Application.Orders;

public interface IOrderService
{
    Task<PagedResult<OrderSummaryDto>> SearchAsync(OrderQueryParams query, CancellationToken ct = default);
    Task<Result<OrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<OrderDto>> GetByTicketAsync(string ticketNumber, CancellationToken ct = default);
    Task<Result<OrderPricePreviewDto>> PreviewPriceAsync(OrderPricePreviewRequest request, CancellationToken ct = default);
    Task<EstimatePickupDateResponse> EstimatePickupDateAsync(EstimatePickupDateRequest request, CancellationToken ct = default);
    Task<Result<OrderDto>> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<Result<OrderItemDto>> UpdateInspectionAsync(Guid orderId, Guid itemId, UpdateOrderItemInspectionRequest request, CancellationToken ct = default);
    Task<Result<ItemPhotoDto>> AddItemPhotoAsync(Guid orderId, Guid itemId, AddItemPhotoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ProductionItemCardDto>> GetProductionQueueAsync(ItemStatus? status = null, string? serviceCode = null, CancellationToken ct = default);
    Task<Result<OrderItemDto>> AdvanceItemStatusAsync(Guid itemId, AdvanceItemStatusRequest request, CancellationToken ct = default);
    Task<Result<QrScanResultDto>> ScanQrAsync(QrScanRequest request, CancellationToken ct = default);
    Task<Result<OrderDto>> PickupAsync(Guid orderId, PickupOrderRequest request, CancellationToken ct = default);
    Task<Result<OrderDto>> CancelAsync(Guid orderId, CancelOrderRequest request, CancellationToken ct = default);
    Task<Result<ClaimDto>> OpenClaimAsync(Guid orderId, CreateClaimRequest request, CancellationToken ct = default);
    Task<Result<ClaimDto>> ResolveClaimAsync(Guid claimId, ResolveClaimRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<StorageLocationDto>> GetStorageLocationsAsync(bool? occupiedOnly = null, string? search = null, CancellationToken ct = default);
    Task<Result<StorageLocationDto>> CreateStorageLocationAsync(CreateStorageLocationRequest request, CancellationToken ct = default);
    Task<Result<OrderItemDto>> AssignStorageAsync(Guid itemId, AssignStorageRequest request, CancellationToken ct = default);
}
