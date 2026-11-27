# Sécurité : exigences et checklist

## Authentification
- [ ] Mots de passe hachés avec sel unique par utilisateur (PBKDF2 d'ASP.NET Core Identity, ou Argon2id). Jamais en clair, jamais dans les journaux.
- [ ] Politique : 12 caractères minimum, mélange recommandé, liste de mots de passe courants refusés.
- [ ] Verrouillage après 5 échecs (15 min), message d'erreur générique.
- [ ] Mot de passe temporaire aléatoire à la création, **changement obligatoire** à la première connexion.
- [ ] JWT de courte durée (15 min) + refresh token à rotation, révocable ; déconnexion après inactivité.
- [ ] **2FA TOTP** (Google Authenticator) : QR d'activation, confirmation par code, 10 codes de secours à usage unique ; obligatoire pour Administrateur et Manager (paramètre).
- [ ] Réinitialisation de mot de passe par lien ou code à durée limitée.

## Autorisation
- [ ] Refus par défaut : chaque endpoint déclare sa politique.
- [ ] Politiques par rôle (`Admin`, `Manager`, `Cashier`, `Operator`, `Driver`, `Customer`).
- [ ] Les contrôles sont **côté serveur** ; cacher un bouton ne suffit pas.
- [ ] Un test automatique vérifie la matrice rôle × endpoint (403 attendu si non autorisé).
- [ ] Portail public : routes minimales, sans données sensibles, protégées par numéro de ticket + code, limitation de débit.

## Données
- [ ] Requêtes paramétrées uniquement (EF Core) ; aucune concaténation SQL.
- [ ] Validation de toute entrée (FluentValidation) ; limites de taille.
- [ ] Upload de fichiers : types autorisés (jpeg, png, webp, pdf), taille maximale, nom régénéré, stockage hors dossier web.
- [ ] Concurrence optimiste (`RowVersion`) sur commandes, paiements, stocks.
- [ ] Journal d'audit non modifiable pour les actions sensibles.
- [ ] Minimisation des données personnelles ; consentement de contact ; export et suppression à la demande du client.

## Transport et configuration
- [ ] HTTPS partout, même en local (mkcert) ; HSTS en production.
- [ ] En-têtes : `Content-Security-Policy`, `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`.
- [ ] CORS restreint aux origines du Web ; protection anti-CSRF sur les formulaires.
- [ ] Limitation de débit (login, OTP, suivi public, envoi de notifications).
- [ ] **Secrets** (clés JWT, WhatsApp, SMS, SMTP, base) : User Secrets en développement, variables d'environnement sur le serveur. Jamais dans Git.
- [ ] Tableau de bord Hangfire accessible aux administrateurs seulement.
- [ ] Sauvegardes chiffrées, restauration testée.

## Si un secret fuit
1. Révoquez-le chez le fournisseur **immédiatement**.
2. Générez-en un nouveau, mettez-le à jour.
3. Prévenez l'équipe. Supprimer le commit ne suffit pas : l'historique Git le contient toujours.

## Revue de sécurité (J6, Dev 1)
Parcourir cette liste, noter les écarts dans des tickets `bug`, corriger avant le gel.
