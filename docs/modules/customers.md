# Module Clients & Contrats B2B (Dev 2 — M03)

## Périmètre couvert
- `CLI-01` à `CLI-07` : CRUD Particulier & Professionnel, recherche instantanée (nom, téléphone, code, N° ticket, QR) avec pagination serveur, préférences de contact (WhatsApp, SMS, e-mail, langue FR/EN, consentement marketing, désinscription), historique complet (commandes, soldes, points, réclamations), contrats B2B (remise négociée %, facturation mensuelle groupée, plafond de crédit), import/export CSV & Excel avec rapport d'erreurs ligne par ligne, détection des doublons et outil de fusion.

## Endpoints API
- `GET/POST /api/customers`, `GET/PUT/DELETE /api/customers/{id}`, `GET /api/customers/{id}/history`, `GET /api/customers/duplicates`
- `PUT /api/customers/{id}/preferences`, `PUT /api/customers/{id}/contract`, `POST /api/customers/merge`
- `GET /api/customers/export/csv|excel`, `GET /api/customers/import/template/csv|excel`, `POST /api/customers/import`
