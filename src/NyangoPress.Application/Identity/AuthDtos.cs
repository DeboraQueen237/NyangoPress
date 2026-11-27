namespace NyangoPress.Application.Identity;

public sealed record LoginRequest(
    string UserNameOrEmail,
    string Password,
    string? TotpCode = null,
    string? RecoveryCode = null);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UserSummary(
    Guid Id,
    string UserName,
    string FullName,
    string? Email,
    string Role,
    string Language,
    string Theme,
    bool MustChangePassword,
    bool TwoFactorEnabled);

/// <summary>
/// Réponse de connexion. Si <see cref="RequiresTwoFactor"/> est vrai, le mot de passe est correct mais il faut
/// renvoyer la même requête avec le code Google Authenticator (ou un code de secours) ; aucun jeton n'est donné.
/// </summary>
public sealed record AuthResponse(
    bool RequiresTwoFactor,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAt,
    string? RefreshToken,
    bool MustChangePassword,
    UserSummary? User);
