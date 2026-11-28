namespace NyangoPress.Domain.Orders;

/// <summary>
/// Strict finite-state machine governing garment item lifecycle transitions.
/// </summary>
public static class ItemStateMachine
{
    private static readonly IReadOnlyDictionary<ItemStatus, ItemStatus[]> AllowedTransitions =
        new Dictionary<ItemStatus, ItemStatus[]>
        {
            [ItemStatus.Received] = [ItemStatus.Sorted, ItemStatus.Cancelled],
            [ItemStatus.Sorted] = [ItemStatus.InTreatment, ItemStatus.Cancelled],
            [ItemStatus.InTreatment] = [ItemStatus.QualityCheck],
            [ItemStatus.QualityCheck] = [ItemStatus.Ready, ItemStatus.InTreatment],
            [ItemStatus.Ready] = [ItemStatus.Delivered, ItemStatus.Unclaimed],
            [ItemStatus.Unclaimed] = [ItemStatus.Delivered, ItemStatus.Auctioned],
            [ItemStatus.Delivered] = [],
            [ItemStatus.Auctioned] = [],
            [ItemStatus.Cancelled] = []
        };

    public static bool CanTransition(ItemStatus from, ItemStatus to)
        => AllowedTransitions.TryGetValue(from, out var next) && next.Contains(to);

    public static IReadOnlyList<ItemStatus> GetAllowedNextStatuses(ItemStatus from)
        => AllowedTransitions.TryGetValue(from, out var next) ? next : [];

    public static ItemStatus? GetDefaultNextStatus(ItemStatus from) => from switch
    {
        ItemStatus.Received => ItemStatus.Sorted,
        ItemStatus.Sorted => ItemStatus.InTreatment,
        ItemStatus.InTreatment => ItemStatus.QualityCheck,
        ItemStatus.QualityCheck => ItemStatus.Ready,
        ItemStatus.Ready => ItemStatus.Delivered,
        ItemStatus.Unclaimed => ItemStatus.Delivered,
        _ => null
    };

    public static void EnsureValidTransition(ItemStatus from, ItemStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Transition from '{from}' to '{to}' is not permitted by ItemStateMachine.");
        }
    }

    public static OrderStatus ComputeOrderStatus(IReadOnlyCollection<ItemStatus> itemStatuses)
    {
        if (itemStatuses.Count == 0)
            return OrderStatus.Received;

        if (itemStatuses.All(s => s == ItemStatus.Cancelled))
            return OrderStatus.Cancelled;

        var active = itemStatuses.Where(s => s != ItemStatus.Cancelled).ToList();
        if (active.Count == 0)
            return OrderStatus.Cancelled;

        if (active.All(s => s == ItemStatus.Delivered))
            return OrderStatus.Delivered;

        if (active.All(s => s is ItemStatus.Unclaimed or ItemStatus.Auctioned))
            return OrderStatus.Unclaimed;

        if (active.Any(s => s is ItemStatus.Unclaimed or ItemStatus.Auctioned))
            return OrderStatus.PartiallyUnclaimed;

        if (active.All(s => s is ItemStatus.Ready or ItemStatus.Delivered))
            return OrderStatus.Ready;

        if (active.Any(s => s is ItemStatus.Ready or ItemStatus.Delivered))
            return OrderStatus.PartiallyReady;

        if (active.All(s => s == ItemStatus.Received))
            return OrderStatus.Received;

        return OrderStatus.InProgress;
    }
}
