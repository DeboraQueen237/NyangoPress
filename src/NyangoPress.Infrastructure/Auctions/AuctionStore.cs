using Microsoft.EntityFrameworkCore;
using NyangoPress.Application.Auctions;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Auctions;
using NyangoPress.Domain.Orders;
using NyangoPress.Infrastructure.Persistence;
using SharedAuctionLotStatus = NyangoPress.Shared.Auctions.AuctionLotStatus;

namespace NyangoPress.Infrastructure.Auctions;

public sealed class AuctionStore(AppDbContext db) : IAuctionStore
{
    public async Task<IReadOnlyList<OrderItem>> GetUnclaimedEligibleItemsAsync(int thresholdDays, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-thresholdDays);
        return await db.OrderItems
            .Include(i => i.Order).ThenInclude(o => o!.Customer)
            .Include(i => i.StatusHistory)
            .Where(i =>
                i.AuctionLotId == null &&
                (i.Status == ItemStatus.Unclaimed ||
                 (i.Status == ItemStatus.Ready && (i.ReadySince ?? i.CreatedAt) <= cutoff)))
            .OrderBy(i => i.ReadySince ?? i.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrderItem>> GetItemsByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
        => await db.OrderItems
            .Include(i => i.Order)
            .Include(i => i.StatusHistory)
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AuctionLot>> GetLotsAsync(SharedAuctionLotStatus? status = null, CancellationToken ct = default)
    {
        var q = db.AuctionLots
            .AsNoTracking()
            .Include(l => l.Items).ThenInclude(i => i.Order)
            .Include(l => l.Bids)
            .AsQueryable();

        if (status.HasValue)
        {
            var ds = status.Value.ToDomain();
            q = q.Where(l => l.Status == ds);
        }
        return await q.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
    }

    public Task<AuctionLot?> GetLotByIdAsync(Guid id, CancellationToken ct = default)
        => db.AuctionLots
            .Include(l => l.Items).ThenInclude(i => i.Order)
            .Include(l => l.Items).ThenInclude(i => i.StatusHistory)
            .Include(l => l.Bids)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task AddLotAsync(AuctionLot lot, CancellationToken ct = default)
        => db.AuctionLots.AddAsync(lot, ct).AsTask();

    public Task AddBidAsync(AuctionBid bid, CancellationToken ct = default)
        => db.AuctionBids.AddAsync(bid, ct).AsTask();
}
