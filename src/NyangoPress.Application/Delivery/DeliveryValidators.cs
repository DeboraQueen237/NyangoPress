using FluentValidation;
using NyangoPress.Shared.Delivery;

namespace NyangoPress.Application.Delivery;

public sealed class UpsertDeliveryZoneValidator : AbstractValidator<UpsertDeliveryZoneRequest>
{
    public UpsertDeliveryZoneValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Districts).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FeeFcfa).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.TimeSlots).NotEmpty().MaximumLength(250);
        RuleFor(x => x.MinimumDelayHours).InclusiveBetween(1, 72);
    }
}

public sealed class CreatePickupRequestValidator : AbstractValidator<CreatePickupRequest>
{
    public CreatePickupRequestValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CustomerPhone).NotEmpty().MinimumLength(6).MaximumLength(30);
        RuleFor(x => x.CustomerEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));
        RuleFor(x => x.ZoneId).NotEmpty();
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PreferredTimeSlot).NotEmpty().MaximumLength(60);
        RuleFor(x => x.EstimatedItemsCount).InclusiveBetween(1, 200);
    }
}

public sealed class BroadcastLocationValidator : AbstractValidator<BroadcastLocationRequest>
{
    public BroadcastLocationValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0);
    }
}
