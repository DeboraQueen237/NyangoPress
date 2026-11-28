namespace NyangoPress.Application.Notifications;

/// <summary>
/// Clés des modèles disponibles. Utilisez ces constantes plutôt que des chaînes en dur.
/// Besoin d'un nouveau modèle ? Demandez à Dev 5 (il crée la clé + les textes FR/EN).
/// Les variables attendues sont listées dans docs/NOTIFICATIONS.md.
/// </summary>
public static class NotificationTemplateKeys
{
    // Dev 1 — comptes et sécurité
    public const string AccountCreated = "account.created";           // UserName, TemporaryPassword, LoginUrl
    public const string PasswordReset = "account.password-reset";      // UserName, ResetUrl, ExpiresInMinutes
    public const string TwoFactorEnabled = "account.2fa-enabled";      // UserName

    // Dev 2 — commandes, retrait, livraison
    public const string OrderCreated = "order.created";                // CustomerName, TicketNumber, ItemCount, EstimatedDate, TrackingUrl
    public const string OrderStatusChanged = "order.status-changed";   // CustomerName, TicketNumber, StatusLabel, TrackingUrl
    public const string OrderReady = "order.ready";                    // CustomerName, TicketNumber, BranchName, OpeningHours
    public const string PickupReminder = "order.pickup-reminder";      // CustomerName, TicketNumber, DaysLeft, BranchName
    public const string OrderAbandoned = "order.abandoned";            // CustomerName, TicketNumber, DaysSinceReady
    public const string DeliveryScheduled = "delivery.scheduled";      // CustomerName, TicketNumber, DeliveryWindow, DriverName
    public const string DeliveryOnTheWay = "delivery.on-the-way";      // CustomerName, TicketNumber, TrackingUrl

    // Dev 3 — paiements et fidélité
    public const string PaymentReceived = "payment.received";          // CustomerName, TicketNumber, Amount, Balance (+ reçu PDF en pièce jointe)
    public const string InvoiceIssued = "invoice.issued";              // CustomerName, InvoiceNumber, Amount, DueDate
    public const string LoyaltyPointsEarned = "loyalty.points-earned"; // CustomerName, Points, Balance

    // Dev 5 — stocks, fournisseurs, clients inactifs
    public const string StockLow = "stock.low";                        // ProductName, Quantity, Unit, Threshold
    public const string StockOut = "stock.out";                        // ProductName
    public const string PurchaseOrderSent = "supplier.purchase-order"; // SupplierName, PurchaseOrderNumber, ExpectedDate (+ PDF)
    public const string InactiveCustomer = "customer.inactive";        // CustomerName, DaysSinceLastVisit
}
