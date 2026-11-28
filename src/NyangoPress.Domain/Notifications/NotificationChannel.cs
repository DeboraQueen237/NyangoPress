namespace NyangoPress.Domain.Notifications;

/// <summary>Canaux de diffusion supportés par le module Notifications (M07).</summary>
public enum NotificationChannel
{
    InApp = 0,
    Email = 1,
    WhatsApp = 2,
    Sms = 3
}

/// <summary>Cycle de vie d'un message dans la table d'envoi (outbox).</summary>
public enum NotificationStatus
{
    Pending = 0,     // en file, pas encore tenté
    Sending = 1,     // pris par l'OutboxProcessor
    Sent = 2,        // accepté par le fournisseur
    Delivered = 3,   // confirmé par webhook (WhatsApp/SMS)
    Failed = 4,      // toutes les tentatives et canaux de secours épuisés
    Cancelled = 5    // annulé (ex. commande retirée avant le rappel)
}

public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2   // ex. identifiants de compte, alerte rupture de stock
}
