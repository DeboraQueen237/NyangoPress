using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Notifications;

/// <summary>Notification affichée dans le centre de notifications (cloche) d'un employé.</summary>
public class InAppNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    /// <summary>Lien relatif dans l'appli, ex. "/orders/42".</summary>
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
