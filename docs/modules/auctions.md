# Module Articles Non Réclamés & Enchères (Dev 2 — INN-01)

## Périmètre couvert
- Détection des articles dépassant le délai de garde paramétrable (défaut 60 jours, `RG-06`) avec calcul automatique des frais de garde journaliers après le délai gratuit (`RG-07`).
- Transition contrôlée vers `Unclaimed` uniquement si les CGV ont été acceptées à la commande (`ORD-11`).
- Création de lots d'enchères (`AuctionLot`), ouverture aux enchères, enregistrement d'offres (`AuctionBid`) avec contrôle du palier minimum, clôture et adjudication (`Auctioned`) avec audit complet.

## Endpoints API (`Policies.AuctionsManage`: ADM, MGR)
- `GET /api/auctions/unclaimed`, `POST /api/auctions/unclaimed/detect`
- `GET/POST /api/auctions/lots`, `GET /api/auctions/lots/{id}`, `POST /api/auctions/lots/{id}/open|bids|award`
