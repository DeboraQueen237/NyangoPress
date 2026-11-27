# Conventions de code

## Nommage (C#)
| Élément | Style | Exemple |
|---|---|---|
| Classes, méthodes, propriétés | PascalCase | `OrderService`, `CalculateTotal` |
| Interfaces | `I` + PascalCase | `IPricingService` |
| Paramètres, variables locales | camelCase | `customerId` |
| Champs privés | `_camelCase` | `_dbContext` |
| Constantes | PascalCase | `MaxLoginAttempts` |
| Méthodes asynchrones | suffixe `Async` | `GetOrderAsync` |
| Fichiers | un type par fichier, même nom | `Order.cs` |

Le **code et les identifiants sont en anglais** ; les **textes visibles et la documentation sont en français et en anglais** via les ressources.

## Règles de code
- `nullable` activé ; pas d'avertissement tolérés (`TreatWarningsAsErrors` en Release).
- `async/await` jusqu'au bout, pas de `.Result` ni `.Wait()`.
- Argent : `decimal` uniquement. Dates : `DateTimeOffset` en UTC, converties à l'affichage.
- Pas de logique métier dans les pages Blazor ni dans les endpoints : dans `Domain` ou `Application`.
- Validation avec FluentValidation à l'entrée de chaque cas d'usage ; ne jamais faire confiance à l'interface.
- Accès aux données avec EF Core : pas de SQL concaténé ; `AsNoTracking()` pour les lectures ; pas de requête dans une boucle ; pagination serveur obligatoire sur les listes.
- Exceptions : levées pour les cas inattendus ; les erreurs métier prévisibles passent par `Result<T>` avec un code d'erreur traduisible.
- Journaux : Serilog, jamais de données personnelles ni de secrets.
- Chaque endpoint déclare sa politique d'autorisation (`RequireAuthorization("Policy")`).

## Structure d'un module (par couche)
```
Domain/Orders/            Entités, enums, règles
Application/Orders/       OrderService, DTO (si internes), Validators, interfaces
Infrastructure/Orders/    Configurations EF, requêtes, implémentations
Api/Endpoints/Orders/     OrderEndpoints.cs (MapGroup("/api/orders"))
Web/Pages/Orders/         Pages et composants du module
```
Chaque module s'enregistre avec une méthode d'extension : `builder.Services.AddOrdersModule()` et `app.MapOrdersEndpoints()`.

## Traductions
- Un fichier de ressources par module : `Orders.fr.resx`, `Orders.en.resx` (clés identiques).
- Clé : `Module.Element.Action`, ex. `Orders.Create.Title`.
- Aucun texte visible en dur dans `.razor`, `.cs` des PDF ou des notifications.

## Interface
- Utiliser les composants `Np*` du design system ; pas de style en ligne sauf exception justifiée.
- Tester à 360, 600, 960 et 1280 px de large, en thème clair et sombre.
- Chaque liste : recherche, tri, pagination serveur, état vide et état de chargement.
- Boutons destructifs : confirmation obligatoire.

## Tests
- Logique métier : tests unitaires xUnit (`MethodName_Condition_ExpectedResult`).
- Endpoints critiques : tests d'intégration avec PostgreSQL via Testcontainers.
- Un bug corrigé = un test qui l'aurait détecté.
