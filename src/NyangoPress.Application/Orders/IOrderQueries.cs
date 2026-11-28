using NyangoPress.Shared.Orders;

namespace NyangoPress.Application.Orders;

public sealed record OrderReminderCandidateDto(
    Guid OrderId,
    string TicketNumber,
    Guid CustomerId,
    string CustomerName,
    DateTimeOffset PromisedAt,
    decimal BalanceFcfa);

public sealed record OrderMetricsSummaryDto(
    int TotalOrders,
    int ActiveOrders,
    int ReadyOrders,
    int DeliveredOrders,
    int OverdueOrders,
    decimal TotalBilledFcfa,
    decimal TotalCollectedFcfa,
    decimal TotalOutstandingFcfa);

/// <summary>
/// Read-only order queries contract owned by Dev 2 for Dev 3 (Reports) and Dev 5 (Jobs).
/// </summary>
public interface IOrderQueries
{
    Task<IReadOnlyList<OrderReminderCandidateDto>> GetOrdersDueForReminderAsync(
        DateTimeOffset targetDateUtc,
        CancellationToken ct = default);

    Task<IReadOnlyList<OrderReminderCandidateDto>> GetOverdueOrdersAsync(
        int minDaysPastPromised,
        CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetInactiveCustomerIdsAsync(
        int inactiveDays,
        CancellationToken ct = default);

    Task<OrderMetricsSummaryDto> GetMetricsSummaryAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken ct = default);
}
