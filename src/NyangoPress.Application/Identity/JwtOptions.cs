namespace NyangoPress.Application.Identity;

/// <summary>
/// Réglages des jetons. La clé (<see cref="Key"/>) ne se met JAMAIS dans Git : User Secrets en développement
/// (<c>dotnet user-secrets set "Jwt:Key" ...</c>), variable d'environnement <c>Jwt__Key</c> en production.
/// En développement, si elle est absente, une clé aléatoire est générée à chaque démarrage.
/// </summary>
public sealed class JwtOptions
{
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "NyangoPress";

    public string Audience { get; set; } = "NyangoPress.Clients";

    /// <summary>Durée de vie du jeton d'accès (court : 15 minutes).</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>
    /// Durée du jeton de rafraîchissement. Renouvelé à chaque usage : sans activité pendant cette durée,
    /// l'utilisateur est déconnecté (inactivité).
    /// </summary>
    public int RefreshTokenHours { get; set; } = 8;
}
