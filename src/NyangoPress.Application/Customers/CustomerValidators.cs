using FluentValidation;
using NyangoPress.Shared.Customers;

namespace NyangoPress.Application.Customers;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Customers.Validation.FullNameRequired")
            .MaximumLength(150);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Customers.Validation.PhoneRequired")
            .MinimumLength(6).WithMessage("Customers.Validation.PhoneInvalid")
            .MaximumLength(30);

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Customers.Validation.EmailInvalid");

        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty().MinimumLength(6).MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpsertCompanyContractValidator : AbstractValidator<UpsertCompanyContractRequest>
{
    public UpsertCompanyContractValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NegotiatedDiscountPercent).InclusiveBetween(0m, 60m);
        RuleFor(x => x.CreditLimitFcfa).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.BillingEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.BillingEmail));
    }
}

public sealed class MergeCustomersValidator : AbstractValidator<MergeCustomersRequest>
{
    public MergeCustomersValidator()
    {
        RuleFor(x => x.PrimaryCustomerId).NotEmpty();
        RuleFor(x => x.DuplicateCustomerId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.PrimaryCustomerId != x.DuplicateCustomerId)
            .WithMessage("Customers.Validation.CannotMergeSameCustomer");
    }
}
