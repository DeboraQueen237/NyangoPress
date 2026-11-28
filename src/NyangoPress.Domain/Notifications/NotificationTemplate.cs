using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Notifications;

/// <summary>
/// Modèle de message pour une clé, un canal et une langue.
/// Ex. Key = "order.ready", Channel = WhatsApp, Language = "fr".
/// Le corps contient des variables {{CustomerName}}, {{TicketNumber}}...
/// </summary>
public class NotificationTemplate : BaseEntity
{
    public string Key { get; set; } = default!;
    public NotificationChannel Channel { get; set; }
    public string Language { get; set; } = "fr";   // "fr" ou "en"

    /// <summary>Objet (e-mail uniquement).</summary>
    public string? Subject { get; set; }
    public string Body { get; set; } = default!;

    /// <summary>Nom du modèle approuvé chez Meta (WhatsApp uniquement).</summary>
    public string? ExternalTemplateName { get; set; }

    public bool IsActive { get; set; } = true;
}
