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
