namespace NyangoPress.Application.Notifications;

/// <summary>
/// CONTRAT PARTAGÉ (propriétaire : Dev 5 — utilisé par Dev 1, 2, 3).
/// Ne pas modifier sans prévenir le groupe et ouvrir une PR dédiée (règle 12.2).
///
/// SendAsync ne contacte PAS directement le fournisseur : il écrit le(s) message(s)
/// dans la table d'envoi puis rend la main. L'envoi réel, les nouvelles tentatives et
/// le canal de secours sont gérés en arrière-plan. L'appel est donc rapide et ne
/// lève pas d'exception si WhatsApp/SMS/SMTP est indisponible.
/// </summary>
public interface INotificationService
{
    /// <summary>Met en file un message sur un ou plusieurs canaux.</summary>
    Task<NotificationReceipt> SendAsync(NotificationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Annule les messages encore en attente pour un identifiant métier
    /// (ex. rappels de retrait d'une commande déjà retirée).
    /// </summary>
    Task<int> CancelPendingAsync(string correlationId, CancellationToken ct = default);
}
