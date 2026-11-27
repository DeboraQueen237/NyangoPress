# Architecture de NyangoPress

Tous les diagrammes sont en Mermaid : ils s'affichent directement sur GitHub.

## 1. Vue d'ensemble

Monolithe modulaire en couches. Chaque module métier (Orders, Payments, Notifications...) a un dossier dans chaque couche.

```mermaid
flowchart TB
  subgraph Presentation
    WEB["NyangoPress.Web<br/>Blazor + MudBlazor"]
    API["NyangoPress.Api<br/>Endpoints, SignalR, Hangfire"]
  end
  APP["NyangoPress.Application<br/>Cas d'usage, interfaces, validateurs"]
  INFRA["NyangoPress.Infrastructure<br/>EF Core, Identity, canaux, PDF, Excel"]
  DOM["NyangoPress.Domain<br/>Entités, règles métier"]
  SHARED["NyangoPress.Shared<br/>Contrats (DTO, constantes)"]
  WEB --> SHARED
  WEB -->|HTTP + SignalR| API
  API --> APP
  API --> INFRA
  API --> SHARED
  INFRA --> APP
  APP --> DOM
  APP --> SHARED
```

**Règles de dépendance :** `Domain` ne dépend de rien. `Application` dépend de `Domain` et `Shared`. `Infrastructure` implémente les interfaces d'`Application`. `Web` ne parle jamais à la base : uniquement à l'API.

## 2. Déploiement sur le réseau local

```mermaid
flowchart LR
  subgraph PC["PC principal (IP fixe)"]
    PG[("PostgreSQL 17")]
    A["API + Hangfire"]
    W["Web Blazor"]
    A --- PG
    W --> A
  end
  K["Poste de caisse"] --> W
  P["Poste de production"] --> W
  T["Tablette"] --> W
  L["Téléphone livreur (PWA)"] --> W
  A -->|SMTP| MAIL["E-mail"]
  A -->|API| WA["WhatsApp Business"]
  A -->|API| SMS["Fournisseur SMS"]
  CLIENT["Client (portail de suivi)"] -->|Tunnel sécurisé, routes publiques seulement| W
```

## 3. Cas d'utilisation

```mermaid
flowchart LR
  ADM(["Administrateur"])
  MGR(["Manager"])
  CAI(["Caissier"])
  OPE(["Opérateur"])
  LIV(["Livreur"])
  CLI(["Client"])

  subgraph Systeme["NyangoPress"]
    U1["Gérer comptes, rôles, 2FA"]
    U2["Consulter l'audit, sauvegarder"]
    U3["Tableau de bord et rapports"]
    U4["Gérer stocks, fournisseurs, commandes fournisseur"]
    U5["Créer une commande, imprimer le ticket"]
    U6["Encaisser, facturer"]
    U7["Faire avancer les articles (scan QR)"]
    U8["Assurer une livraison"]
    U9["Suivre sa commande"]
    U10["Consulter points et jetons"]
    U11["Recevoir des notifications"]
  end

  ADM --> U1
  ADM --> U2
  MGR --> U3
  MGR --> U4
  MGR --> U1
  CAI --> U5
  CAI --> U6
  OPE --> U7
  LIV --> U8
  CLI --> U9
  CLI --> U10
  CLI --> U11
```

## 4. États d'un article

```mermaid
stateDiagram-v2
  [*] --> Recu
  Recu --> Trie
  Recu --> Annule
  Trie --> EnTraitement
  EnTraitement --> ControleQualite
  ControleQualite --> EnTraitement : refusé
  ControleQualite --> Pret
  Pret --> Livre : retrait ou livraison
  Pret --> NonReclame : délai dépassé
  NonReclame --> Livre : client récupère
  NonReclame --> VenduEncheres
  Livre --> [*]
  VenduEncheres --> [*]
  Annule --> [*]
```

`EnTraitement` contient les sous-étapes détachage, lavage ou nettoyage à sec, séchage, repassage.
La machine d'états est codée dans `Domain/Orders/ItemStateMachine.cs` et testée de façon exhaustive : toute transition non listée est refusée.

## 5. Modèle de données (noyau)

```mermaid
erDiagram
  CUSTOMER ||--o{ ORDER : passe
  ORDER ||--|{ ORDER_ITEM : contient
  ORDER_ITEM ||--o{ ITEM_PHOTO : possede
  ORDER_ITEM ||--o{ ITEM_STATUS_HISTORY : trace
  ORDER ||--o{ PAYMENT : regle
  ORDER ||--o| INVOICE : genere
  ORDER ||--o| DELIVERY_ORDER : livre
  CUSTOMER ||--|| LOYALTY_ACCOUNT : possede
  LOYALTY_ACCOUNT ||--o{ LOYALTY_LEDGER_ENTRY : enregistre
  GARMENT_TYPE ||--o{ PRICE_ENTRY : tarife
  SERVICE_TYPE ||--o{ PRICE_ENTRY : tarife
  ORDER_ITEM }o--|| GARMENT_TYPE : est
  ORDER_ITEM }o--|| SERVICE_TYPE : subit
  APP_USER ||--|| EMPLOYEE_PROFILE : decrit
  APP_USER ||--o{ AUDIT_LOG : produit
  SUPPLIER ||--o{ PURCHASE_ORDER : recoit
  PURCHASE_ORDER ||--|{ PURCHASE_ORDER_LINE : contient
  PRODUCT ||--o{ STOCK_MOVEMENT : subit
  NOTIFICATION_MESSAGE ||--o{ NOTIFICATION_ATTEMPT : tente

  CUSTOMER { guid Id string FullName string Phone string Email string Language bool IsCompany }
  ORDER { guid Id string TicketNumber guid CustomerId datetime PromisedAt decimal Total decimal Paid string Status }
  ORDER_ITEM { guid Id guid OrderId string QrCode string Status string DefectNotes decimal Price }
  PAYMENT { guid Id guid OrderId decimal Amount string Method string Reference }
  LOYALTY_LEDGER_ENTRY { guid Id guid AccountId string Kind int Points int Tokens datetime ExpiresAt }
```

Toutes les entités métier ont : `Id`, `BranchId`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `RowVersion` (concurrence optimiste).

## 6. Séquence : dépôt d'une commande

```mermaid
sequenceDiagram
  actor Caissier
  participant Web as Web (caisse)
  participant API
  participant Prix as IPricingService
  participant DB as PostgreSQL
  participant Bus as DomainEventDispatcher
  participant Notif as NotificationService
  actor Client

  Caissier->>Web: Saisit articles, services, photos
  Web->>API: POST /orders
  API->>Prix: CalculateAsync(draft)
  Prix-->>API: Prix détaillé
  API->>DB: Enregistre commande + articles (transaction)
  API->>Bus: OrderCreated
  Bus->>Notif: Reçu et ticket
  Notif->>Client: WhatsApp ou e-mail (PDF)
  API-->>Web: Commande + ticket
  Web-->>Caissier: Impression du ticket
```

## 7. Séquence : notification fiable

```mermaid
sequenceDiagram
  participant M as Module (ex. Orders)
  participant O as Outbox (table)
  participant J as Job Hangfire
  participant W as Canal WhatsApp
  participant S as Canal SMS

  M->>O: Enregistre le message (même transaction)
  J->>O: Lit les messages en attente
  J->>W: Envoi
  alt échec ou pas de réseau
    W--xJ: Erreur
    J->>O: Nouvelle tentative plus tard
    J->>S: Canal de secours après N échecs
  else succès
    W-->>J: Accepté
    J->>O: Statut = envoyé
  end
```

## 8. Classes principales (extrait)

```mermaid
classDiagram
  class BaseEntity { Guid Id; Guid BranchId; DateTime CreatedAt; byte[] RowVersion }
  class Order { string TicketNumber; DateTime PromisedAt; decimal Total; AddItem(); Cancel(); }
  class OrderItem { string QrCode; ItemStatus Status; Advance(); }
  class Payment { decimal Amount; PaymentMethod Method }
  class LoyaltyAccount { int Points; int Tokens; Tier Tier }
  class INotificationService { <<interface>> SendAsync() }
  class IPricingService { <<interface>> CalculateAsync() }
  BaseEntity <|-- Order
  BaseEntity <|-- OrderItem
  BaseEntity <|-- Payment
  Order "1" --> "*" OrderItem
  Order "1" --> "*" Payment
```

## 9. Communication entre modules

Les modules ne s'appellent pas directement dans les deux sens. Pour éviter les dépendances circulaires :

- **Appel synchrone** (réponse immédiate nécessaire) : via une interface déclarée dans `Application` (ex. `IPricingService`).
- **Événement** (réaction sans attendre) : via `IDomainEventDispatcher` (ex. `OrderStatusChanged` écouté par Notifications).
- **Lecture** : via une interface de requêtes en lecture seule (ex. `IOrderQueries`).

## 10. Décisions d'architecture

| Décision | Raison |
|---|---|
| Web + API (pas WPF) | Responsive, accessible depuis tout appareil, portail client possible |
| Blazor + MudBlazor | Même langage C# partout, composants graphiques riches, productivité |
| PostgreSQL | Fiable en réseau local, recherche rapide (pg_trgm), sauvegardes simples |
| Monolithe modulaire | Équipe de 5, 7 jours : simple à déployer et à déboguer |
| Outbox + Hangfire | Aucune notification perdue si le réseau tombe |
| Registre de fidélité immuable | Soldes vérifiables, pas de triche possible |
| `decimal` pour l'argent | Pas d'erreur d'arrondi |
| `BranchId` partout dès J1 | Multi-succursales sans refonte |
