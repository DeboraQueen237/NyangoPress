using Microsoft.EntityFrameworkCore;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Orders;
using NyangoPress.Infrastructure.Persistence;

namespace NyangoPress.Infrastructure.Common;

public sealed class SequenceGenerator(AppDbContext db) : ISequenceGenerator
{
    public async Task<string> NextTicketNumberAsync(CancellationToken ct = default)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var next = await NextValueAsync($"ORDER_TICKET_{year}", ct);
        return $"NP-{year}-{next:D4}";
    }

    public async Task<string> NextCustomerCodeAsync(CancellationToken ct = default)
    {
        var next = await NextValueAsync("CUSTOMER_CODE", ct);
        return $"CLI-{next:D4}";
    }

    public async Task<string> NextTourCodeAsync(CancellationToken ct = default)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var next = await NextValueAsync($"DELIVERY_TOUR_{year}", ct);
        return $"TRN-{year}-{next:D3}";
    }

    public async Task<string> NextPickupReferenceAsync(CancellationToken ct = default)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var next = await NextValueAsync($"PICKUP_REQ_{year}", ct);
        return $"COL-{year}-{next:D4}";
    }

    public async Task<string> NextAuctionLotNumberAsync(CancellationToken ct = default)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var next = await NextValueAsync($"AUCTION_LOT_{year}", ct);
        return $"LOT-{year}-{next:D3}";
    }

    private async Task<long> NextValueAsync(string key, CancellationToken ct)
    {
        var counter = await db.SequenceCounters.FirstOrDefaultAsync(c => c.Key == key, ct);
        if (counter is null)
        {
            counter = new SequenceCounter { Key = key, LastValue = 1 };
            db.SequenceCounters.Add(counter);
        }
        else
        {
            counter.LastValue++;
        }

        await db.SaveChangesAsync(ct);
        return counter.LastValue;
    }
}
