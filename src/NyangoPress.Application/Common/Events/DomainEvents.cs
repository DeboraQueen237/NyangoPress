namespace NyangoPress.Application.Common.Events;

// Événements partagés (contrat du jour 1). Les propriétaires de module peuvent AJOUTER des propriétés
// par PR (jamais en supprimer ni en renommer sans prévenir les consommateurs).
// Les statuts sont des chaînes pour ne pas dépendre des enums du module Commandes.

public sealed record OrderCreated : DomainEvent
{
    public Guid OrderId { get; init; }

    public Guid CustomerId { get; init; }

    public string TicketNumber { get; init; } = string.Empty;

    public Guid BranchId { get; init; }

    public Guid? CreatedBy { get; init; }
}

public sealed record OrderStatusChanged : DomainEvent
{
    public Guid OrderId { get; init; }

    public Guid? OrderItemId { get; init; }

    public string FromStatus { get; init; } = string.Empty;

    public string ToStatus { get; init; } = string.Empty;

    public Guid? ChangedBy { get; init; }
}

public sealed record PaymentReceived : DomainEvent
{
    public Guid PaymentId { get; init; }

    public Guid OrderId { get; init; }

    public Guid CustomerId { get; init; }

    public decimal Amount { get; init; }

    /// <summary>Vrai quand la commande est entièrement payée (déclenche les points de fidélité, RG-08).</summary>
    public bool OrderFullyPaid { get; init; }
}

public sealed record DeliveryStatusChanged : DomainEvent
{
    public Guid DeliveryOrderId { get; init; }

    public Guid OrderId { get; init; }

    public string Status { get; init; } = string.Empty;
}

public sealed record StockBelowThreshold : DomainEvent
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public decimal CurrentQuantity { get; init; }

    public decimal MinimumQuantity { get; init; }
}

/// <summary>
/// Un compte employé vient d'être créé (EMP-02). Dev 5 envoie les identifiants par WhatsApp ou e-mail.
/// Contient le mot de passe temporaire : ne jamais le journaliser ni le copier ailleurs que dans le message.
/// </summary>
public sealed record UserCreated : DomainEvent
{
    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string Language { get; init; } = "fr";

    public string TemporaryPassword { get; init; } = string.Empty;
}
