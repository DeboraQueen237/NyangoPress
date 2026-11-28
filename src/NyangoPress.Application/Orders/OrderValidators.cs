using FluentValidation;
using NyangoPress.Shared.Orders;

namespace NyangoPress.Application.Orders;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.AcceptedTermsAndConditions)
            .Equal(true)
            .WithMessage("Orders.Validation.TermsMustBeAccepted");
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Orders.Validation.AtLeastOneItemRequired");
        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemValidator());
        RuleFor(x => x.InitialDepositFcfa).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.DeliveryAddress)
            .NotEmpty()
            .When(x => x.DeliveryOption != DeliveryOption.StorePickup)
            .WithMessage("Orders.Validation.DeliveryAddressRequired");
    }
}

public sealed class CreateOrderItemValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemValidator()
    {
        RuleFor(x => x.GarmentCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ServiceCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.Brand).MaximumLength(80);
        RuleFor(x => x.DefectNotes).MaximumLength(500);
    }
}

public sealed class PickupOrderValidator : AbstractValidator<PickupOrderRequest>
{
    public PickupOrderValidator()
    {
        RuleFor(x => x.PaymentCollectedFcfa).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.OverrideReason)
            .NotEmpty()
            .When(x => x.OverrideUnpaidBalance)
            .WithMessage("Orders.Validation.OverrideReasonRequired");
    }
}

public sealed class CancelOrderValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(500);
    }
}

public sealed class CreateClaimValidator : AbstractValidator<CreateClaimRequest>
{
    public CreateClaimValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
    }
}

public sealed class ResolveClaimValidator : AbstractValidator<ResolveClaimRequest>
{
    public ResolveClaimValidator()
    {
        RuleFor(x => x.ResolutionNotes).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.GoodwillGestureFcfa)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.GoodwillGestureFcfa.HasValue);
    }
}

public sealed class CreateStorageLocationValidator : AbstractValidator<CreateStorageLocationRequest>
{
    public CreateStorageLocationValidator()
    {
        RuleFor(x => x.Aisle).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Rack).NotEmpty().MaximumLength(10);
        RuleFor(x => x.HangerNumber).NotEmpty().MaximumLength(10);
    }
}
