using NyangoPress.Domain.Common;
using NyangoPress.Domain.Orders;

namespace NyangoPress.Domain.Auctions;

public class AuctionLot : BaseEntity
{
    public string LotNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AuctionLotStatus Status { get; set; } = AuctionLotStatus.Draft;
    public decimal StartingPriceFcfa { get; set; }
    public decimal CurrentHighestBidFcfa { get; set; }
    public decimal MinimumIncrementFcfa { get; set; } = 500m;

    public string? WinningBidderName { get; set; }
    public string? WinningBidderPhone { get; set; }
    public decimal? FinalSalePriceFcfa { get; set; }
    public DateTimeOffset? OpensAt { get; set; }
    public DateTimeOffset ClosesAt { get; set; }
    public DateTimeOffset? AwardedAt { get; set; }
    public string? AwardNotes { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<AuctionBid> Bids { get; set; } = new List<AuctionBid>();
}
