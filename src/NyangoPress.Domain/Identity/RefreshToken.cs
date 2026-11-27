namespace NyangoPress.Domain.Identity;

/// <summary>
/// Jeton de rafraîchissement. Seule l'empreinte (SHA-256) est stockée : une fuite de la base
/// ne permet donc pas de se connecter. Rotation à chaque usage, révocable (AUTH-05).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public string? CreatedByIp { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? RevokedReason { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
