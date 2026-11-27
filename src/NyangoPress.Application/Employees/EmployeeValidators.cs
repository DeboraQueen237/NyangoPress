using FluentValidation;
using NyangoPress.Domain.Identity;

namespace NyangoPress.Application.Employees;

internal static class EmployeeRules
{
    public const string PhonePattern = @"^\+?[0-9 ()\-]{8,20}$";

    public static bool NotInFuture(DateOnly? date) =>
        date is null || date.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
}

public sealed class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().Matches("^[A-Za-z0-9._-]{3,50}$")
            .WithMessage("Identifiant : 3 à 50 caractères (lettres, chiffres, point, tiret, souligné).");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role).Must(r => Roles.Staff.Contains(r)).WithMessage("Rôle invalide.");
        RuleFor(x => x.PhoneNumber).Matches(EmployeeRules.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.WhatsAppNumber).Matches(EmployeeRules.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.WhatsAppNumber));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Language).Must(l => l is "fr" or "en").WithMessage("Langue : fr ou en.");
        RuleFor(x => x.HireDate).Must(EmployeeRules.NotInFuture).WithMessage("La date d'embauche ne peut pas être dans le futur.");
    }
}

public sealed class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role).Must(r => Roles.Staff.Contains(r)).WithMessage("Rôle invalide.");
        RuleFor(x => x.PhoneNumber).Matches(EmployeeRules.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.WhatsAppNumber).Matches(EmployeeRules.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.WhatsAppNumber));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.HireDate).Must(EmployeeRules.NotInFuture).WithMessage("La date d'embauche ne peut pas être dans le futur.");
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).Matches(EmployeeRules.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.WhatsAppNumber).Matches(EmployeeRules.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.WhatsAppNumber));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Language).Must(l => l is "fr" or "en").WithMessage("Langue : fr ou en.");
        RuleFor(x => x.Theme).Must(t => t is "light" or "dark" or "system").WithMessage("Thème : light, dark ou system.");
    }
}
