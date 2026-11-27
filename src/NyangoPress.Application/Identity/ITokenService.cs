namespace NyangoPress.Application.Identity;

/// <summary>Données mises dans le jeton d'accès.</summary>
public sealed record TokenSubject(
    Guid UserId,
    string UserName,
    string FullName,
    string Role,
    Guid BranchId,
    bool MustChangePassword);

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    /// <summary>Crée un jeton JWT signé (HS256) de courte durée.</summary>
    AccessToken CreateAccessToken(TokenSubject subject);

    /// <summary>Génère un jeton de rafraîchissement aléatoire (à donner au client, jamais à stocker tel quel).</summary>
    string GenerateRefreshToken();

    /// <summary>Empreinte SHA-256 d'un jeton de rafraîchissement : c'est elle qui est stockée en base.</summary>
    string HashRefreshToken(string refreshToken);
}
