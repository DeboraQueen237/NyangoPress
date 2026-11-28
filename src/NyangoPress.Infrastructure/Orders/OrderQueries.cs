using Microsoft.EntityFrameworkCore;
using NyangoPress.Application.Orders;
using NyangoPress.Domain.Orders;
using NyangoPress.Infrastructure.Persistence;

namespace NyangoPress.Infrastructure.Orders;

public sealed class OrderQueries(AppDbContext db) : IOrderQueries
{
    public async Task<IReadOnlyList<OrderReminderCandidateDto>> GetOrdersDueForReminderAsync(
        DateTimeOffset targetDateUtc,
        CancellationToken ct = default)
    {
        var dayStart = new DateTimeOffset(targetDateUtc.UtcDateTime.Date, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var candidates = await db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Where(o => o.Status != OrderStatus.Delivered &&
                        o.Status != OrderStatus.Cancelled &&
                        o.PromisedAt >= dayStart &&
                        o.PromisedAt < dayEnd)
            .ToListAsync(ct);

        return candidates.Select(o => new OrderReminderCandidateDto(
            o.Id,
            o.TicketNumber,
            o.CustomerId,
            o.Customer?.FullName ?? string.Empty,
            o.PromisedAt,
            o.BalanceFcfa)).ToList();
    }

    public async Task<IReadOnlyList<OrderReminderCandidateDto>> GetOverdueOrdersAsync(
        int minDaysPastPromised,
        CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-minDaysPastPromised);
        var orders = await db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Where(o => o.Status != OrderStatus.Delivered &&
                        o.Status != OrderStatus.Cancelled &&
                        o.Status != OrderStatus.Unclaimed &&
                        o.PromisedAt < cutoff)
            .ToListAsync(ct);

        return orders.Select(o => new OrderReminderCandidateDto(
            o.Id,
            o.TicketNumber,
            o.CustomerId,
            o.Customer?.FullName ?? string.Empty,
            o.PromisedAt,
            o.BalanceFcfa)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetInactiveCustomerIdsAsync(int inactiveDays, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-inactiveDays);
        return await db.Customers
            .AsNoTracking()
            .Where(c => !c.Orders.Any(o => o.CreatedAt >= cutoff))
            .Select(c => c.Id)
            .ToListAsync(ct);
    }

    public async Task<OrderMetricsSummaryDto> GetMetricsSummaryAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken ct = default)
    {
        var q = db.Orders.AsNoTracking().AsQueryable();
        if (fromUtc.HasValue) q = q.Where(o => o.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(o => o.CreatedAt <= toUtc.Value);

        var orders = await q.ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;

        return new OrderMetricsSummaryDto(
            orders.Count,
            orders.Count(o => o.Status is OrderStatus.Received or OrderStatus.InProgress or OrderStatus.PartiallyReady),
            orders.Count(o => o.Status == OrderStatus.Ready),
            orders.Count(o => o.Status == OrderStatus.Delivered),
            orders.Count(o => o.Status is not OrderStatus.Delivered and not OrderStatus.Cancelled && o.PromisedAt < now),
            orders.Sum(o => o.TotalFcfa + o.StorageFeeFcfa),
            orders.Sum(o => o.PaidFcfa),
            orders.Sum(o => o.BalanceFcfa));
    }
}
