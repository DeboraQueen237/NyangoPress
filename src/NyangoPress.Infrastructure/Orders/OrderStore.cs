using Microsoft.EntityFrameworkCore;
using NyangoPress.Application.Common;
using NyangoPress.Application.Orders;
using NyangoPress.Domain.Delivery;
using NyangoPress.Domain.Orders;
using NyangoPress.Infrastructure.Persistence;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Orders;
using DomainItemStatus = NyangoPress.Domain.Orders.ItemStatus;
using SharedItemStatus = NyangoPress.Shared.Orders.ItemStatus;

namespace NyangoPress.Infrastructure.Orders;

public sealed class OrderStore(AppDbContext db) : IOrderStore
{
    public Task<Order?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken ct = default)
    {
        IQueryable<Order> q = db.Orders;
        if (includeDetails)
        {
            q = q.Include(o => o.Customer)
                 .Include(o => o.Claims)
                 .Include(o => o.Items).ThenInclude(i => i.Photos)
                 .Include(o => o.Items).ThenInclude(i => i.StatusHistory)
                 .Include(o => o.Items).ThenInclude(i => i.StorageLocation);
        }
        return q.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public Task<Order?> GetByTicketAsync(string ticketNumber, bool includeDetails = true, CancellationToken ct = default)
    {
        IQueryable<Order> q = db.Orders;
        if (includeDetails)
        {
            q = q.Include(o => o.Customer)
                 .Include(o => o.Claims)
                 .Include(o => o.Items).ThenInclude(i => i.Photos)
                 .Include(o => o.Items).ThenInclude(i => i.StatusHistory)
                 .Include(o => o.Items).ThenInclude(i => i.StorageLocation);
        }
        return q.FirstOrDefaultAsync(o => o.TicketNumber == ticketNumber, ct);
    }

    public Task<OrderItem?> GetItemByIdAsync(Guid itemId, CancellationToken ct = default)
        => db.OrderItems
            .Include(i => i.Order).ThenInclude(o => o!.Customer)
            .Include(i => i.Photos)
            .Include(i => i.StatusHistory)
            .Include(i => i.StorageLocation)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

    public Task<OrderItem?> GetItemByQrAsync(string qrCode, CancellationToken ct = default)
        => db.OrderItems
            .Include(i => i.Order).ThenInclude(o => o!.Customer)
            .Include(i => i.Photos)
            .Include(i => i.StatusHistory)
            .Include(i => i.StorageLocation)
            .FirstOrDefaultAsync(i => i.QrCode == qrCode, ct);

    public async Task<PagedResult<OrderSummaryDto>> SearchAsync(OrderQueryParams query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .AsQueryable();

        if (query.Status.HasValue)
        {
            var domainStatus = query.Status.Value.ToDomain();
            q = q.Where(o => o.Status == domainStatus);
        }

        if (query.ExpressOnly == true)
            q = q.Where(o => o.IsExpress);

        if (query.CustomerId.HasValue)
            q = q.Where(o => o.CustomerId == query.CustomerId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(o =>
                o.TicketNumber.Contains(term) ||
                (o.Customer != null && (o.Customer.FullName.Contains(term) || o.Customer.Phone.Contains(term))) ||
                o.Items.Any(i => i.QrCode.Contains(term)));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(o => o.IsExpress)
            .ThenByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.TicketNumber,
                o.TrackingCode,
                o.CustomerId,
                o.Customer != null ? o.Customer.FullName : string.Empty,
                o.Customer != null ? o.Customer.Phone : string.Empty,
                o.Status.ToShared(),
                o.DeliveryOption.ToShared(),
                o.IsExpress,
                o.Items.Count,
                o.Items.Count(i => i.Status == DomainItemStatus.Ready),
                o.TotalFcfa + o.StorageFeeFcfa,
                o.PaidFcfa,
                Math.Max(0m, o.TotalFcfa + o.StorageFeeFcfa - o.PaidFcfa),
                o.PromisedAt,
                o.CreatedAt,
                o.RowVersion))
            .ToListAsync(ct);

        return new PagedResult<OrderSummaryDto>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<ProductionItemCardDto>> GetProductionQueueAsync(
        SharedItemStatus? status = null,
        string? serviceCode = null,
        CancellationToken ct = default)
    {
        var q = db.OrderItems
            .AsNoTracking()
            .Include(i => i.Order).ThenInclude(o => o!.Customer)
            .Include(i => i.StorageLocation)
            .Include(i => i.Photos)
            .Where(i => i.Status != DomainItemStatus.Delivered && i.Status != DomainItemStatus.Cancelled && i.Status != DomainItemStatus.Auctioned);

        if (status.HasValue)
        {
            var domainStatus = status.Value.ToDomain();
            q = q.Where(i => i.Status == domainStatus);
        }

        if (!string.IsNullOrWhiteSpace(serviceCode))
            q = q.Where(i => i.ServiceCode == serviceCode.Trim().ToUpperInvariant());

        var rows = await q
            .OrderByDescending(i => i.IsExpress)
            .ThenBy(i => i.Order!.PromisedAt)
            .ToListAsync(ct);

        return rows.Select(i => new ProductionItemCardDto(
            i.Id,
            i.OrderId,
            i.Order?.TicketNumber ?? string.Empty,
            i.QrCode,
            i.Order?.Customer?.FullName ?? string.Empty,
            i.GarmentName,
            i.ServiceName,
            i.Status.ToShared(),
            i.TreatmentSubStep.ToShared(),
            i.IsExpress,
            i.IsBulky,
            i.Color,
            i.Brand,
            i.DefectNotes,
            i.StorageLocation?.Code,
            i.Order?.PromisedAt ?? i.CreatedAt,
            i.Photos.Count,
            i.RowVersion)).ToList();
    }

    public Task<int> CountActiveWorkshopItemsAsync(CancellationToken ct = default)
        => db.OrderItems.CountAsync(
            i => i.Status == DomainItemStatus.Received ||
                 i.Status == DomainItemStatus.Sorted ||
                 i.Status == DomainItemStatus.InTreatment ||
                 i.Status == DomainItemStatus.QualityCheck,
            ct);

    public async Task<IReadOnlyList<StorageLocation>> GetStorageLocationsAsync(
        bool? occupiedOnly = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var q = db.StorageLocations.AsNoTracking().AsQueryable();
        if (occupiedOnly.HasValue)
            q = q.Where(l => l.IsOccupied == occupiedOnly.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(l => l.Code.Contains(search.Trim().ToUpperInvariant()));

        return await q.OrderBy(l => l.Aisle).ThenBy(l => l.Rack).ThenBy(l => l.HangerNumber).ToListAsync(ct);
    }

    public Task<StorageLocation?> GetStorageLocationByIdAsync(Guid id, CancellationToken ct = default)
        => db.StorageLocations.FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<bool> StorageLocationCodeExistsAsync(string code, CancellationToken ct = default)
        => db.StorageLocations.AnyAsync(l => l.Code == code, ct);

    public Task AddStorageLocationAsync(StorageLocation location, CancellationToken ct = default)
        => db.StorageLocations.AddAsync(location, ct).AsTask();

    public Task AddOrderAsync(Order order, CancellationToken ct = default)
        => db.Orders.AddAsync(order, ct).AsTask();

    public Task AddDeliveryOrderAsync(DeliveryOrder deliveryOrder, CancellationToken ct = default)
        => db.DeliveryOrders.AddAsync(deliveryOrder, ct).AsTask();

    public Task<DeliveryZone?> GetDeliveryZoneByIdAsync(Guid zoneId, CancellationToken ct = default)
        => db.DeliveryZones.FirstOrDefaultAsync(z => z.Id == zoneId, ct);

    public Task<Claim?> GetClaimByIdAsync(Guid claimId, CancellationToken ct = default)
        => db.Claims.FirstOrDefaultAsync(c => c.Id == claimId, ct);

    public Task AddClaimAsync(Claim claim, CancellationToken ct = default)
        => db.Claims.AddAsync(claim, ct).AsTask();
}
