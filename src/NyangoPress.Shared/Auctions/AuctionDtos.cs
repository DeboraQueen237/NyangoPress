using NyangoPress.Shared.Orders;

namespace NyangoPress.Shared.Auctions;

public enum AuctionLotStatus
{
    Draft = 0,
    Open = 1,
    Closed = 2,
    Awarded = 3,
    Cancelled = 4
}

public sealed record UnclaimedCandidateDto(
    Guid OrderItemId,
    Guid OrderId,
    string TicketNumber,
    string QrCode,
    string CustomerName,
    string CustomerPhone,
    string GarmentName,
    string ServiceName,
    ItemStatus CurrentStatus,
    DateTimeOffset ReadySince,
    int DaysSinceReady,
    decimal OriginalOrderBalanceFcfa,
    decimal AccruedStorageFeeFcfa,
    bool TermsAccepted);

public sealed record AuctionBidDto(
    Guid Id,
    Guid AuctionLotId,
    string BidderName,
    string BidderPhone,
    decimal AmountFcfa,
    DateTimeOffset PlacedAt,
    bool IsWinning);

public sealed record AuctionLotItemDto(
    Guid OrderItemId,
    string TicketNumber,
    string QrCode,
    string GarmentName,
    string ServiceName,
    string? Color,
    string? Brand);

public sealed record AuctionLotDto(
    Guid Id,
    string LotNumber,
    string Title,
    string? Description,
    AuctionLotStatus Status,
    decimal StartingPriceFcfa,
    decimal CurrentHighestBidFcfa,
    decimal MinimumIncrementFcfa,
    string? WinningBidderName,
    string? WinningBidderPhone,
    decimal? FinalSalePriceFcfa,
    DateTimeOffset? OpensAt,
    DateTimeOffset ClosesAt,
    DateTimeOffset? AwardedAt,
    IReadOnlyList<AuctionLotItemDto> Items,
    IReadOnlyList<AuctionBidDto> Bids,
    long RowVersion);

public sealed record DetectUnclaimedRequest(
    int UnclaimedThresholdDays = 60,
    int FreeStorageDays = 7,
    decimal DailyStorageFeeFcfa = 200m,
    bool ApplyStatusTransition = true);

public sealed record DetectUnclaimedResultDto(
    int MarkedUnclaimedCount,
    IReadOnlyList<UnclaimedCandidateDto> Candidates);

public sealed record CreateAuctionLotRequest(
    string Title,
    string? Description,
    decimal StartingPriceFcfa,
    decimal MinimumIncrementFcfa,
    DateTimeOffset ClosesAt,
    bool OpenImmediately,
    IReadOnlyList<Guid> OrderItemIds);

public sealed record PlaceBidRequest(
    string BidderName,
    string BidderPhone,
    decimal AmountFcfa);

public sealed record AwardAuctionLotRequest(
    string? Notes);
