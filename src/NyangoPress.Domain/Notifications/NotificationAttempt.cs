using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Notifications;

/// <summary>Trace de chaque tentative d'envoi (journal d'envoi, NOT : relance manuelle).</summary>
public class NotificationAttempt : BaseEntity
{
    public Guid NotificationMessageId { get; set; }
    public NotificationMessage NotificationMessage { get; set; } = default!;

    public int AttemptNumber { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    /// <summary>Réponse brute du fournisseur, tronquée à 2000 caractères.</summary>
    public string? ProviderResponse { get; set; }
}
