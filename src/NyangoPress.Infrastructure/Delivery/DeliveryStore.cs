using Microsoft.EntityFrameworkCore;
using NyangoPress.Application.Common;
using NyangoPress.Application.Delivery;
using NyangoPress.Domain.Delivery;
using NyangoPress.Domain.Orders;
using NyangoPress.Infrastructure.Persistence;
using SharedDeliveryStatus = NyangoPress.Shared.Delivery.DeliveryStatus;
using SharedPickupRequestStatus = NyangoPress.Shared.Delivery.PickupRequestStatus;

namespace NyangoPress.Infrastructure.Delivery;

public sealed class DeliveryStore(AppDbContext db) : IDeliveryStore
{
    public async Task<IReadOnlyList<DeliveryZone>> GetZonesAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var q = db.DeliveryZones.AsNoTracking().AsQueryable();
        if (activeOnly) q = q.Where(z => z.IsActive);
        return await q.OrderBy(z => z.Name).ToListAsync(ct);
    }

    public Task<DeliveryZone?> GetZoneByIdAsync(Guid id, CancellationToken ct = default)
        => db.DeliveryZones.FirstOrDefaultAsync(z => z.Id == id, ct);

    public Task AddZoneAsync(DeliveryZone zone, CancellationToken ct = default)
        => db.DeliveryZones.AddAsync(zone, ct).AsTask();

    public async Task<IReadOnlyList<DeliveryOrder>> GetDeliveriesAsync(SharedDeliveryStatus? status = null, Guid? driverId = null, CancellationToken ct = default)
    {
        var q = db.DeliveryOrders
            .AsNoTracking()
            .Include(d => d.Order).ThenInclude(o => o!.Customer)
            .Include(d => d.Zone)
            .AsQueryable();

        if (status.HasValue)
        {
            var ds = status.Value.ToDomain();
            q = q.Where(d => d.Status == ds);
        }
        if (driverId.HasValue) q = q.Where(d => d.DriverId == driverId.Value);

        return await q.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);
    }

    public Task<DeliveryOrder?> GetDeliveryByIdAsync(Guid id, CancellationToken ct = default)
        => db.DeliveryOrders
            .Include(d => d.Order).ThenInclude(o => o!.Customer)
            .Include(d => d.Order).ThenInclude(o => o!.Items).ThenInclude(i => i.StatusHistory)
            .Include(d => d.Zone)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<DeliveryOrder?> GetDeliveryByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => db.DeliveryOrders
            .Include(d => d.Zone)
            .FirstOrDefaultAsync(d => d.OrderId == orderId, ct);

    public async Task<IReadOnlyList<DeliveryTour>> GetToursAsync(DateOnly? date = null, Guid? driverId = null, CancellationToken ct = default)
    {
        var q = db.DeliveryTours
            .AsNoTracking()
            .Include(t => t.Stops).ThenInclude(s => s.Order).ThenInclude(o => o!.Customer)
            .Include(t => t.Stops).ThenInclude(s => s.Zone)
            .AsQueryable();

        if (date.HasValue) q = q.Where(t => t.TourDate == date.Value);
        if (driverId.HasValue) q = q.Where(t => t.DriverId == driverId.Value);

        return await q.OrderByDescending(t => t.TourDate).ThenByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public Task<DeliveryTour?> GetTourByIdAsync(Guid id, CancellationToken ct = default)
        => db.DeliveryTours
            .Include(t => t.Stops).ThenInclude(s => s.Order).ThenInclude(o => o!.Customer)
            .Include(t => t.Stops).ThenInclude(s => s.Zone)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task AddTourAsync(DeliveryTour tour, CancellationToken ct = default)
        => db.DeliveryTours.AddAsync(tour, ct).AsTask();

    public Task<DriverLocation?> GetLatestDriverLocationAsync(Guid driverId, CancellationToken ct = default)
        => db.DriverLocations
            .AsNoTracking()
            .Where(l => l.DriverId == driverId)
            .OrderByDescending(l => l.RecordedAt)
            .FirstOrDefaultAsync(ct);

    public Task AddDriverLocationAsync(DriverLocation location, CancellationToken ct = default)
        => db.DriverLocations.AddAsync(location, ct).AsTask();

    public async Task<IReadOnlyList<PickupRequest>> GetPickupRequestsAsync(SharedPickupRequestStatus? status = null, CancellationToken ct = default)
    {
        var q = db.PickupRequests
            .AsNoTracking()
            .Include(p => p.Zone)
            .AsQueryable();

        if (status.HasValue)
        {
            var ps = status.Value.ToDomain();
            q = q.Where(p => p.Status == ps);
        }
        return await q.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public Task<PickupRequest?> GetPickupRequestByIdAsync(Guid id, CancellationToken ct = default)
        => db.PickupRequests.Include(p => p.Zone).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task AddPickupRequestAsync(PickupRequest request, CancellationToken ct = default)
        => db.PickupRequests.AddAsync(request, ct).AsTask();

    public Task<Order?> GetOrderByTicketAndCodeAsync(string ticketNumber, string trackingCode, CancellationToken ct = default)
        => db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.StatusHistory)
            .Include(o => o.Items).ThenInclude(i => i.StorageLocation)
            .Include(o => o.DeliveryOrder).ThenInclude(d => d!.Zone)
            .FirstOrDefaultAsync(o => o.TicketNumber == ticketNumber && o.TrackingCode == trackingCode, ct);
}
