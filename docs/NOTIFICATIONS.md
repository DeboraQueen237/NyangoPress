# Notifications : canaux, modèles, comptes

## Principe
1. Un module émet un **événement** (ex. `OrderStatusChanged`).
2. Un gestionnaire demande un envoi : `INotificationService.SendAsync(NotificationRequest)`.
3. Le moteur choisit les **canaux** selon les préférences et le consentement du client, rend le **modèle** dans sa **langue**, et écrit le message dans la table d'envoi (**outbox**), dans la même transaction que l'événement.
4. Un job **Hangfire** envoie, enregistre chaque tentative, relance avec attente croissante, et bascule vers un **canal de secours** après plusieurs échecs (WhatsApp → SMS → e-mail).

## Canaux
| Canal | Fournisseur proposé | Notes |
|---|---|---|
| E-mail | SMTP via MailKit (Mailpit en développement) | PDF en pièce jointe |
| SMS | Africa's Talking ou Twilio | Coût par message ; expéditeur à déclarer |
| WhatsApp | WhatsApp Business Cloud API (Meta) ou Twilio | **Modèles approuvés** obligatoires pour écrire en premier |
| In-app | SignalR | Cloche et compteur pour le personnel |
| Fake | Classe locale | Écrit dans les journaux ; sert aux tests et au développement |

## Contrat d'interface (à publier en J1)
```csharp
public enum NotificationChannel { InApp, Email, Sms, WhatsApp }

public sealed record NotificationRequest(
    string TemplateKey,                       // ex. "order.ready"
    Guid? CustomerId,                         // destinataire client
    Guid? UserId,                             // destinataire employé
    string? Email, string? Phone,             // cibles directes (fournisseur, etc.)
    IReadOnlyDictionary<string, string> Variables,
    IReadOnlyList<NotificationAttachment>? Attachments = null,
    NotificationChannel[]? ForcedChannels = null,
    DateTimeOffset? SendAt = null);

public interface INotificationService
{
    Task EnqueueAsync(NotificationRequest request, CancellationToken ct = default);
}
```

## Modèles (clés principales)
| Clé | Quand |
|---|---|
| `order.received` | Dépôt (avec reçu PDF) |
| `order.inprogress`, `order.quality`, `order.ready` | Étapes |
| `order.reminder.d2`, `.d1`, `.d0` | Rappels de retrait |
| `order.overdue.d1`, `.d7`, `.d30`, `.final` | Relances d'abandon (avis avant vente aux enchères) |
| `delivery.assigned`, `.enroute`, `.near`, `.delivered` | Livraison (avec code à 4 chiffres) |
| `payment.received`, `payment.due` | Paiements |
| `loyalty.earned`, `.tier`, `.expiring` | Fidélité |
| `customer.winback` | Client inactif |
| `staff.account.created`, `staff.password.reset` | Comptes employés |
| `stock.low`, `stock.out` | Alertes stock au manager |
| `purchase.sent`, `purchase.received` | Bons de commande |

Chaque clé existe en **FR et EN**. Variables : `{{customer_name}}`, `{{ticket}}`, `{{ready_date}}`, `{{amount}}`, `{{tracking_url}}`, etc. Les modèles sont modifiables par le manager.

## Rappels et relances (paramétrables)
- Heure d'envoi des rappels : 09:00 (par défaut).
- Relances d'abandon : J+1, J+7, J+30, avis final ; passage « Non réclamé » à J+60 (valeurs par défaut).
- Aucune relance si la commande est livrée, annulée ou si le client s'est désinscrit de tous les canaux marketing (les messages transactionnels restent envoyés sur au moins un canal).

## Comptes à créer (J1)
1. **Meta for Developers** → application WhatsApp Business ; numéro dédié ; compte entreprise vérifié ; soumettre les modèles de messages (catégorie *utility*). **Délai de validation : prévoir plusieurs jours.**
2. **SMS** : compte Africa's Talking (ou Twilio), clé d'API, identifiant d'expéditeur.
3. **SMTP** : Gmail (mot de passe d'application), Brevo ou autre.

En attendant la validation, développez avec le canal **Fake** ou le bac à sable du fournisseur ; l'interface est la même, seul le fournisseur change.

## Tests à faire
- Coupure d'internet : les messages restent en file et partent au retour.
- Client sans WhatsApp : bascule SMS puis e-mail.
- Langue du client respectée sur tous les canaux.
- Aucun doublon d'envoi après un redémarrage du serveur (clé d'idempotence).

## Comment envoyer un message (Dev 1, 2, 3)

Injectez `INotificationService` et appelez `SendAsync` :

```csharp
public class EmployeeService(INotificationService notifications) { ... }

await notifications.SendAsync(new NotificationRequest
{
    TemplateKey = NotificationTemplateKeys.AccountCreated,
    Recipient   = NotificationRecipient.ForUser(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Language),
    Variables   = new Dictionary<string, string>
    {
        ["UserName"] = user.UserName!,
        ["TemporaryPassword"] = tempPassword,
        ["LoginUrl"] = loginUrl
    },
    Channels = [NotificationChannel.Email],
    Priority = NotificationPriority.High
}, ct);
```

Points importants :

- **Rapide et sans risque** : `SendAsync` met le message en file (table d'envoi) et rend la main. Il ne lève pas d'exception si WhatsApp, SMS ou SMTP est en panne ; les nouvelles tentatives et le canal de secours sont automatiques.
- **Canaux** : laissez `Channels` vide pour que le module choisisse selon les préférences du client. Un canal refusé par le client n'est jamais utilisé.
- **Langue** : passez `Recipient.Language` (`"fr"` ou `"en"`) ; le bon modèle est choisi automatiquement.
- **Montants** : formatez-les avant de les passer (ex. `"12 500 FCFA"`). Le module ne calcule rien.
- **Pièces jointes** : enregistrez d'abord le fichier avec `IFileStorage` (ex. le reçu de `IDocumentService`), puis passez sa clé dans `NotificationAttachment`.
- **Anti-doublon** : renseignez `CorrelationId` (ex. `$"payment:{payment.Id}"`) pour qu'un événement rejoué n'envoie pas deux fois.
- **Envoi différé** : `ScheduledFor` pour un rappel. `CancelPendingAsync(correlationId)` pour annuler les rappels d'une commande déjà retirée.

## Modèles disponibles et variables attendues

| Clé | Demandé par | Variables |
|---|---|---|
| `account.created` | Dev 1 | UserName, TemporaryPassword, LoginUrl |
| `account.password-reset` | Dev 1 | UserName, ResetUrl, ExpiresInMinutes |
| `account.2fa-enabled` | Dev 1 | UserName |
| `order.created` | Dev 2 | CustomerName, TicketNumber, ItemCount, EstimatedDate, TrackingUrl |
| `order.status-changed` | Dev 5 (événement de Dev 2) | CustomerName, TicketNumber, StatusLabel, TrackingUrl |
| `order.ready` | Dev 5 (événement de Dev 2) | CustomerName, TicketNumber, BranchName, OpeningHours |
| `order.pickup-reminder` | Dev 5 (job) | CustomerName, TicketNumber, DaysLeft, BranchName |
| `order.abandoned` | Dev 5 (job, RG-06) | CustomerName, TicketNumber, DaysSinceReady |
| `delivery.scheduled` | Dev 2 | CustomerName, TicketNumber, DeliveryWindow, DriverName |
| `delivery.on-the-way` | Dev 2 | CustomerName, TicketNumber, TrackingUrl |
| `payment.received` | Dev 3 | CustomerName, TicketNumber, Amount, Balance (+ reçu PDF) |
| `invoice.issued` | Dev 3 | CustomerName, InvoiceNumber, Amount, DueDate |
| `loyalty.points-earned` | Dev 3 | CustomerName, Points, Balance |
| `stock.low` / `stock.out` | Dev 5 | ProductName, Quantity, Unit, Threshold |
| `supplier.purchase-order` | Dev 5 | SupplierName, PurchaseOrderNumber, ExpectedDate (+ PDF) |
| `customer.inactive` | Dev 5 (job) | CustomerName, DaysSinceLastVisit |

Besoin d'un autre modèle ? Demandez à Dev 5 : il ajoute la clé et les textes FR/EN.

## Dans les tests

En J1, `FakeNotificationService` est enregistré par défaut (`Notifications:UseFake = true`). Il n'envoie rien et garde les demandes en mémoire :

```csharp
var fake = factory.Services.GetRequiredService<FakeNotificationService>();
Assert.Contains(fake.Sent, r => r.TemplateKey == NotificationTemplateKeys.AccountCreated);
```

## Architecture (implémentation réelle, J2–J4)

```
SendAsync ──► notification_messages (Pending) ──► OutboxProcessor (Hangfire, chaque minute)
                                                      │
                                     EmailChannel / WhatsAppChannel / SmsChannel / InApp (SignalR)
                                                      │
                     échec ► nouvelle tentative (1 min, 5 min, 30 min) ► canal de secours ► Failed
                     succès ► Sent ► webhook fournisseur ► Delivered
```
