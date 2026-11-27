namespace NyangoPress.Application.Employees;

public sealed record EmployeeListItem(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt);

public sealed record EmployeeDetails(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt,
    string? WhatsAppNumber,
    string? Address,
    string Language,
    DateOnly? HireDate,
    string? Notes,
    bool HasPhoto,
    DateTimeOffset CreatedAt);

public sealed record EmployeeQuery(string? Search = null, string? Role = null, bool? IsActive = null, int Page = 1, int PageSize = 20);

public sealed record CreateEmployeeRequest(
    string UserName,
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber = null,
    string? WhatsAppNumber = null,
    string? Address = null,
    string Language = "fr",
    DateOnly? HireDate = null);

public sealed record UpdateEmployeeRequest(
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber = null,
    string? WhatsAppNumber = null,
    string? Address = null,
    DateOnly? HireDate = null,
    string? Notes = null);

/// <summary>Résultat interne de la création : contient le mot de passe temporaire (jamais journalisé).</summary>
public sealed record CreatedEmployee(EmployeeDetails Employee, string TemporaryPassword)
{
    public override string ToString() => nameof(CreatedEmployee);
}

/// <summary>Réponse HTTP : le mot de passe n'est renvoyé qu'en développement (Account:ExposeTemporaryPassword).</summary>
public sealed record CreateEmployeeResponse(EmployeeDetails Employee, string? TemporaryPassword);

public sealed record ProfileDto(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    string? WhatsAppNumber,
    string? Address,
    string Language,
    string Theme,
    DateOnly? HireDate,
    bool HasPhoto);

public sealed record UpdateProfileRequest(
    string FullName,
    string? PhoneNumber,
    string? WhatsAppNumber,
    string? Address,
    string Language,
    string Theme);

public sealed record PhotoFile(Stream Content, string ContentType);
