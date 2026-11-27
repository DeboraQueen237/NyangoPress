# Module Identité et sécurité (Dev 1)

## Endpoints
| Méthode | Route | Accès |
|---|---|---|
| POST | `/api/auth/login` | public, 10/min/IP |
| POST | `/api/auth/refresh` | public, 10/min/IP |
| POST | `/api/auth/logout` | connecté |
| POST | `/api/auth/change-password` | connecté (même mot de passe temporaire) |
| GET | `/api/auth/me` | connecté |
| GET | `/health/live`, `/health/ready` | public |

## Règles
- Mot de passe : 12 caractères min., majuscule + minuscule + chiffre, mots de passe courants refusés, pas l'identifiant dedans.
- 5 échecs -> compte verrouillé 15 minutes. Messages d'erreur génériques.
- Jeton d'accès JWT 15 min. Jeton de rafraîchissement 8 h, **rotation à chaque usage** ; un jeton réutilisé coupe toutes les sessions.
- Compte avec mot de passe temporaire : toutes les routes refusent (403) sauf changement de mot de passe, déconnexion, `/me`.
- Tout endpoint est protégé par défaut ; public = `.AllowAnonymous()` justifié en revue.

## Pour les autres développeurs
- Droits : `.RequireAuthorization(Policies.CreateOrder)` (voir `Application/Common/Policies.cs`).
- Utilisateur courant : injectez `ICurrentUser`.
- Votre module : classe `IModule` dans `Api/Endpoints/<Module>/` (services + endpoints). Ne touchez pas à `Program.cs`.
- Entités : héritez de `BaseEntity`, configuration via `BaseEntityConfiguration<T>`. L'audit est automatique.
- Données de départ : `ISeeder`. Événements : `IDomainEventDispatcher`. Fichiers : `IFileStorage`.

## Secrets (jamais dans Git)
`dotnet user-secrets set "ConnectionStrings:Default" "..."` ; `Seed:AdminEmail` ; `Seed:AdminPassword` ; `Jwt:Key` (optionnel en dev).
