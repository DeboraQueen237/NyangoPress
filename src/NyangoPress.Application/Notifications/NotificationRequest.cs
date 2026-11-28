using NyangoPress.Domain.Notifications;

namespace NyangoPress.Application.Notifications;

/// <summary>
/// Demande d'envoi. Exemple (Dev 1, création de compte) :
/// <code>
/// await notifications.SendAsync(new NotificationRequest
/// {
///     TemplateKey = NotificationTemplateKeys.AccountCreated,
///     Recipient = NotificationRecipient.ForUser(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Language),
///     Variables = new Dictionary&lt;string, string&gt;
///     {
///         ["UserName"] = user.UserName!,
///         ["TemporaryPassword"] = tempPassword
///     },
///     Channels = [NotificationChannel.Email],
///     Priority = NotificationPriority.High
/// }, ct);
/// </code>
/// </summary>
public sealed record NotificationRequest
{
    /// <summary>Clé du modèle, voir <see cref="NotificationTemplateKeys"/>.</summary>
    public required string TemplateKey { get; init; }

    public required NotificationRecipient Recipient { get; init; }

    /// <summary>Valeurs des variables {{Nom}} du modèle. Montants déjà formatés côté appelant.</summary>
    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Canaux souhaités. Null ou vide = résolution automatique selon les préférences
    /// du client (un canal refusé par le client n'est JAMAIS utilisé, même s'il est listé ici).
    /// </summary>
    public IReadOnlyList<NotificationChannel>? Channels { get; init; }

    public IReadOnlyList<NotificationAttachment> Attachments { get; init; } = [];

    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;

    /// <summary>
    /// Identifiant métier anti-doublon, ex. "order:{id}:ready". Deux demandes avec le même
    /// CorrelationId et le même modèle ne produisent qu'un envoi.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>Envoi différé (rappels J-2, J-1...). Null = dès que possible.</summary>
    public DateTimeOffset? ScheduledFor { get; init; }
}

public sealed record NotificationRecipient
{
    public Guid? CustomerId { get; init; }
    public Guid? UserId { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    /// <summary>Format E.164 (+237...), utilisé pour SMS et WhatsApp.</summary>
    public string? Phone { get; init; }
    /// <summary>"fr" ou "en".</summary>
    public string Language { get; init; } = "fr";

    public static NotificationRecipient ForCustomer(Guid customerId, string name, string? email, string? phone, string language = "fr")
        => new() { CustomerId = customerId, Name = name, Email = email, Phone = phone, Language = language };

    public static NotificationRecipient ForUser(Guid userId, string name, string? email, string? phone, string language = "fr")
        => new() { UserId = userId, Name = name, Email = email, Phone = phone, Language = language };
}

/// <summary>
/// Pièce jointe déjà enregistrée via IFileStorage (Dev 1), ex. un reçu PDF de IDocumentService.
/// On passe une clé et non des octets pour que la table d'envoi reste légère.
/// </summary>
public sealed record NotificationAttachment(string StorageKey, string FileName, string ContentType = "application/pdf");

public sealed record NotificationReceipt(IReadOnlyList<Guid> MessageIds, IReadOnlyList<NotificationChannel> Channels)
{
    public bool Queued => MessageIds.Count > 0;
    public static NotificationReceipt Empty { get; } = new([], []);
}
