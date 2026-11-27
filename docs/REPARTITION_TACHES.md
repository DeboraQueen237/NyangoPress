# Répartition des tâches (résumé)

> Version détaillée jour par jour, avec les fichiers à produire : voir `NyangoPress_Plan_Action_Repartition.pdf`.

| Dév | Rôle | Modules | Branches |
|---|---|---|---|
| **Dev 1** | Lead technique, sécurité, fondations | Authentification, employés et badges, audit, paramètres, sauvegarde | `core-`, `sec-`, `emp-` |
| **Dev 2** | Cœur métier | Clients, commandes, production, livraison, enchères, API du suivi public | `cli-`, `ord-`, `del-`, `auction-` |
| **Dev 3** | Finance, documents, fidélité | Catalogue, prix, paiements, PDF, fidélité, rapports, imports et exports | `cat-`, `pay-`, `loy-`, `rpt-` |
| **Dev 4** | Interface, multilingue | Design system, i18n, caisse, kanban, tableaux de bord, portail, application livreur, PWA | `ui-`, `i18n-`, `portal-` |
| **Dev 5** | Notifications et logistique | Notifications, stocks, fournisseurs, équipements, tâches planifiées, assistant détachage, prévisions | `not-`, `stk-`, `sup-`, `jobs-` |

## Jalons
| Jour | Objectif |
|---|---|
| J1 | Fondations, CI, contrats, design system |
| J2 | Connexion par rôle, clients, catalogue, moteur de notification |
| J3 | Commandes complètes, paiements, tickets PDF |
| J4 | **Fin des P0** : parcours dépôt → retrait de bout en bout |
| J5 | Livraison, fidélité, 2FA, rapports, inventaire |
| J6 | Finitions, enchères, innovations, i18n complet — **gel des fonctionnalités** |
| J7 | Stabilisation, version 1.0 |
