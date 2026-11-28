using FluentAssertions;
using NyangoPress.Application.Orders;
using NyangoPress.Domain.Orders;
using NyangoPress.Shared.Orders;
using DomainItemStatus = NyangoPress.Domain.Orders.ItemStatus;

namespace NyangoPress.UnitTests.Orders;

public class OrderCancellationTests
{
    [Fact]
    public void CancelOrderValidator_ShortOrEmptyReason_FailsValidation()
    {
        var validator = new CancelOrderValidator();

        validator.Validate(new CancelOrderRequest("")).IsValid.Should().BeFalse();
        validator.Validate(new CancelOrderRequest("abc")).IsValid.Should().BeFalse();
        validator.Validate(new CancelOrderRequest("Erreur de saisie du caissier")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ItemCancellation_AllowedOnlyBeforeTreatmentStarts()
    {
        ItemStateMachine.CanTransition(DomainItemStatus.Received, DomainItemStatus.Cancelled).Should().BeTrue();
        ItemStateMachine.CanTransition(DomainItemStatus.Sorted, DomainItemStatus.Cancelled).Should().BeTrue();

        ItemStateMachine.CanTransition(DomainItemStatus.InTreatment, DomainItemStatus.Cancelled).Should().BeFalse();
        ItemStateMachine.CanTransition(DomainItemStatus.QualityCheck, DomainItemStatus.Cancelled).Should().BeFalse();
        ItemStateMachine.CanTransition(DomainItemStatus.Ready, DomainItemStatus.Cancelled).Should().BeFalse();
        ItemStateMachine.CanTransition(DomainItemStatus.Delivered, DomainItemStatus.Cancelled).Should().BeFalse();
    }

    [Fact]
    public void PickupOrderValidator_OverrideWithoutReason_FailsValidation()
    {
        var validator = new PickupOrderValidator();

        validator.Validate(new PickupOrderRequest(0m, null, OverrideUnpaidBalance: true, OverrideReason: null))
            .IsValid.Should().BeFalse();

        validator.Validate(new PickupOrderRequest(0m, null, OverrideUnpaidBalance: true, OverrideReason: "Accord client B2B"))
            .IsValid.Should().BeTrue();
    }
}
