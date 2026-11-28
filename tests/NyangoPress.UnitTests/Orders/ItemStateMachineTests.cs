using FluentAssertions;
using NyangoPress.Domain.Orders;

namespace NyangoPress.UnitTests.Orders;

public class ItemStateMachineTests
{
    public static IEnumerable<object[]> AllTransitions()
    {
        var validPairs = new HashSet<(ItemStatus, ItemStatus)>
        {
            (ItemStatus.Received, ItemStatus.Sorted),
            (ItemStatus.Received, ItemStatus.Cancelled),
            (ItemStatus.Sorted, ItemStatus.InTreatment),
            (ItemStatus.Sorted, ItemStatus.Cancelled),
            (ItemStatus.InTreatment, ItemStatus.QualityCheck),
            (ItemStatus.QualityCheck, ItemStatus.Ready),
            (ItemStatus.QualityCheck, ItemStatus.InTreatment),
            (ItemStatus.Ready, ItemStatus.Delivered),
            (ItemStatus.Ready, ItemStatus.Unclaimed),
            (ItemStatus.Unclaimed, ItemStatus.Delivered),
            (ItemStatus.Unclaimed, ItemStatus.Auctioned)
        };

        foreach (var from in Enum.GetValues<ItemStatus>())
        {
            foreach (var to in Enum.GetValues<ItemStatus>())
            {
                yield return [from, to, validPairs.Contains((from, to))];
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllTransitions))]
    public void CanTransition_AllStatePairs_MatchesStrictSpecification(
        ItemStatus from,
        ItemStatus to,
        bool expectedAllowed)
    {
        ItemStateMachine.CanTransition(from, to).Should().Be(expectedAllowed);
    }

    [Fact]
    public void EnsureValidTransition_IllegalTransition_ThrowsInvalidOperationException()
    {
        var act = () => ItemStateMachine.EnsureValidTransition(ItemStatus.Received, ItemStatus.Delivered);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ComputeOrderStatus_MixedItems_ComputesAccurateAggregateStatus()
    {
        ItemStateMachine.ComputeOrderStatus([ItemStatus.Received, ItemStatus.Received])
            .Should().Be(OrderStatus.Received);

        ItemStateMachine.ComputeOrderStatus([ItemStatus.Received, ItemStatus.InTreatment])
            .Should().Be(OrderStatus.InProgress);

        ItemStateMachine.ComputeOrderStatus([ItemStatus.Ready, ItemStatus.InTreatment])
            .Should().Be(OrderStatus.PartiallyReady);

        ItemStateMachine.ComputeOrderStatus([ItemStatus.Ready, ItemStatus.Cancelled])
            .Should().Be(OrderStatus.Ready);

        ItemStateMachine.ComputeOrderStatus([ItemStatus.Delivered, ItemStatus.Delivered])
            .Should().Be(OrderStatus.Delivered);

        ItemStateMachine.ComputeOrderStatus([ItemStatus.Cancelled, ItemStatus.Cancelled])
            .Should().Be(OrderStatus.Cancelled);

        ItemStateMachine.ComputeOrderStatus([ItemStatus.Unclaimed, ItemStatus.Delivered])
            .Should().Be(OrderStatus.PartiallyUnclaimed);
    }
}
