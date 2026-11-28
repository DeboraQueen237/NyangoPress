using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Auctions;

public class AuctionBid : BaseEntity
{
    public Guid AuctionLotId { get; set; }
    public AuctionLot? AuctionLot { get; set; }
    public string BidderName { get; set; } = string.Empty;
    public string BidderPhone { get; set; } = string.Empty;
    public decimal AmountFcfa { get; set; }
    public DateTimeOffset PlacedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsWinning { get; set; }
}
