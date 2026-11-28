# Module Livraison, Suivi Temps Réel & Portail Public (Dev 2 — M09, M14)

## Périmètre couvert
- `DEL-01` à `DEL-07` : Options livraison/collecte, zones et tarifs FCFA, affectation des livreurs et tournées ordonnées, application livreur mobile-first (`/driver`), preuve de livraison par code OTP à 4 chiffres (`RG-09`), photo ou signature, diffusion GPS en direct via SignalR (`DeliveryHub`), et demandes de collecte en ligne.
- `POR-01` : Portail public `/suivi` protégé par couple `TicketNumber + TrackingCode` sans fuite de données personnelles (nom masqué), avec carte Leaflet + OpenStreetMap en temps réel et limitation de débit.

## Endpoints API
- Staff : `GET/POST/PUT /api/delivery/zones`, `GET /api/delivery/missions`, `POST /api/delivery/missions/{id}/assign|status|complete`, `GET/POST /api/delivery/tours`, `PUT /api/delivery/tours/{id}/reorder`, `POST /api/delivery/tours/{id}/start`, `POST /api/delivery/location`, `GET /api/delivery/pickup-requests`, `POST /api/delivery/pickup-requests/{id}/review`.
- Public (AllowAnonymous + RateLimit) : `GET /api/public/tracking/{ticketNumber}?code=...`, `GET /api/public/zones`, `POST /api/public/pickup-requests`.
- SignalR : `/hubs/delivery`
