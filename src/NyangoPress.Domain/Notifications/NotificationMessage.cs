using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Notifications;

/// <summary>
/// Une ligne de la table d'envoi (outbox). Un message = un destinataire + un canal.
/// Rien n'est envoyé directement : on écrit ici, l'OutboxProcessor (Hangfire) envoie.
/// C'est ce qui garantit « aucun message perdu si le réseau tombe ».
/// </summary>
public class NotificationMessage : BaseEntity
{
    public string TemplateKey { get; set; } = default!;
    public NotificationChannel Channel { get; set; }
    public string Language { get; set; } = "fr";

    // Destinataire
    public Guid? CustomerId { get; set; }
    public Guid? UserId { get; set; }
    public string RecipientName { get; set; } = default!;
    /// <summary>E-mail, numéro E.164 (+237...) ou Id utilisateur pour InApp.</summary>
    public string RecipientAddress { get; set; } = default!;

    // Contenu déjà rendu (on garde ce qui a réellement été envoyé)
    public string? Subject { get; set; }
    public string Body { get; set; } = default!;
    /// <summary>Variables sérialisées en JSON (utile pour les modèles WhatsApp).</summary>
    public string VariablesJson { get; set; } = "{}";
    /// <summary>Clés IFileStorage des pièces jointes, séparées par ';'.</summary>
    public string? AttachmentKeys { get; set; }

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>Identifiant métier pour éviter les doublons (ex. "order:42:ready").</summary>
    public string? CorrelationId { get; set; }

    public DateTimeOffset? ScheduledFor { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? LastError { get; set; }

    /// <summary>Si ce message est un canal de secours, Id du message d'origine.</summary>
    public Guid? FallbackOfMessageId { get; set; }

    public List<NotificationAttempt> Attempts { get; set; } = new();
}
