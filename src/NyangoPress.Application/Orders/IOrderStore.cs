using NyangoPress.Domain.Delivery;
using NyangoPress.Domain.Orders;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Orders;
using DomainItemStatus = NyangoPress.Domain.Orders.ItemStatus;
using SharedItemStatus = NyangoPress.Shared.Orders.ItemStatus;

namespace NyangoPress.Application.Orders;

public interface IOrderStore
{
    Task<Order?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken ct = default);
    Task<Order?> GetByTicketAsync(string ticketNumber, bool includeDetails = true, CancellationToken ct = default);
    Task<OrderItem?> GetItemByIdAsync(Guid itemId, CancellationToken ct = default);
    Task<OrderItem?> GetItemByQrAsync(string qrCode, CancellationToken ct = default);
    Task<PagedResult<OrderSummaryDto>> SearchAsync(OrderQueryParams query, CancellationToken ct = default);
    Task<IReadOnlyList<ProductionItemCardDto>> GetProductionQueueAsync(SharedItemStatus? status = null, string? serviceCode = null, CancellationToken ct = default);
    Task<int> CountActiveWorkshopItemsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StorageLocation>> GetStorageLocationsAsync(bool? occupiedOnly = null, string? search = null, CancellationToken ct = default);
    Task<StorageLocation?> GetStorageLocationByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> StorageLocationCodeExistsAsync(string code, CancellationToken ct = default);
    Task AddStorageLocationAsync(StorageLocation location, CancellationToken ct = default);
    Task AddOrderAsync(Order order, CancellationToken ct = default);
    Task AddDeliveryOrderAsync(DeliveryOrder deliveryOrder, CancellationToken ct = default);
    Task<DeliveryZone?> GetDeliveryZoneByIdAsync(Guid zoneId, CancellationToken ct = default);
    Task<Claim?> GetClaimByIdAsync(Guid claimId, CancellationToken ct = default);
    Task AddClaimAsync(Claim claim, CancellationToken ct = default);
}

public interface IQrCodeService
{
    string GenerateSvg(string payload, int pixelsPerModule = 4);
    byte[] GeneratePng(string payload, int pixelsPerModule = 6);
}
