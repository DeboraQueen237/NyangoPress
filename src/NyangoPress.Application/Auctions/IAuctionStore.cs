using NyangoPress.Domain.Auctions;
using NyangoPress.Domain.Orders;
using SharedAuctionLotStatus = NyangoPress.Shared.Auctions.AuctionLotStatus;

namespace NyangoPress.Application.Auctions;

public interface IAuctionStore
{
    Task<IReadOnlyList<OrderItem>> GetUnclaimedEligibleItemsAsync(int thresholdDays, CancellationToken ct = default);
    Task<IReadOnlyList<OrderItem>> GetItemsByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<AuctionLot>> GetLotsAsync(SharedAuctionLotStatus? status = null, CancellationToken ct = default);
    Task<AuctionLot?> GetLotByIdAsync(Guid id, CancellationToken ct = default);
    Task AddLotAsync(AuctionLot lot, CancellationToken ct = default);
    Task AddBidAsync(AuctionBid bid, CancellationToken ct = default);
}
