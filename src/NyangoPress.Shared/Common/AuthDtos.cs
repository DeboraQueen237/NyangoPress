namespace NyangoPress.Shared.Common;

public sealed record LoginRequest(string Identifier, string Password);

public sealed record DemoLoginRequest(string RoleCode, string? DisplayName = null);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserInfoDto User);

public sealed record UserInfoDto(
    Guid Id,
    string DisplayName,
    string Email,
    string Role,
    Guid BranchId,
    string DefaultRoute);
