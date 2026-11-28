using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Auctions;

public sealed record AuctionLotAwardedEvent(
    Guid AuctionLotId,
    string LotNumber,
    string? WinnerName,
    decimal FinalPriceFcfa) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
