using System.Collections.Concurrent;

namespace NyangoPress.Web.Localization;

public sealed class LanguageService
{
    public string CurrentLanguage { get; private set; } = "fr";
    public event Action? OnChange;

    public bool IsFrench => CurrentLanguage == "fr";
    public bool IsEnglish => CurrentLanguage == "en";

    public void SetLanguage(string lang)
    {
        var normalized = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "fr";
        if (CurrentLanguage == normalized) return;
        CurrentLanguage = normalized;
        OnChange?.Invoke();
    }

    public string T(string key)
    {
        if (Translations.TryGetValue(key, out var pair))
            return IsFrench ? pair.Fr : pair.En;
        return key;
    }

    public string T(string key, params object[] args)
    {
        var template = T(key);
        return args.Length == 0 ? template : string.Format(template, args);
    }

    private static readonly ConcurrentDictionary<string, (string Fr, string En)> Translations = new(new Dictionary<string, (string, string)>
    {
        // Navigation & App
        ["App.Tagline"] = ("Le Pressing Connecté", "The Connected Dry Cleaner"),
        ["Nav.Home"] = ("Accueil", "Home"),
        ["Nav.NewOrder"] = ("Nouveau dépôt (Caisse)", "New Deposit (POS)"),
        ["Nav.Orders"] = ("Commandes", "Orders"),
        ["Nav.Customers"] = ("Clients", "Customers"),
        ["Nav.Production"] = ("File de production", "Production Kanban"),
        ["Nav.Storage"] = ("Plan de stockage", "Storage Rack Map"),
        ["Nav.Delivery"] = ("Livraisons & Tournées", "Delivery & Tours"),
        ["Nav.Driver"] = ("App Livreur (PWA)", "Driver App (PWA)"),
        ["Nav.Auctions"] = ("Non réclamés & Enchères", "Unclaimed & Auctions"),
        ["Nav.PublicPortal"] = ("Suivi client public", "Public Tracking Portal"),
        ["Nav.Login"] = ("Connexion", "Sign in"),
        ["Nav.Logout"] = ("Déconnexion", "Sign out"),

        // Common
        ["Common.Search"] = ("Rechercher...", "Search..."),
        ["Common.Save"] = ("Enregistrer", "Save"),
        ["Common.Cancel"] = ("Annuler", "Cancel"),
        ["Common.Close"] = ("Fermer", "Close"),
        ["Common.Delete"] = ("Supprimer", "Delete"),
        ["Common.Edit"] = ("Modifier", "Edit"),
        ["Common.View"] = ("Détails", "Details"),
        ["Common.Refresh"] = ("Actualiser", "Refresh"),
        ["Common.ExportCsv"] = ("Exporter CSV", "Export CSV"),
        ["Common.ExportExcel"] = ("Exporter Excel", "Export Excel"),
        ["Common.Import"] = ("Importer", "Import"),
        ["Common.Loading"] = ("Chargement...", "Loading..."),
        ["Common.Empty"] = ("Aucune donnée disponible", "No data available"),
        ["Common.Confirm"] = ("Confirmer", "Confirm"),
        ["Common.Actions"] = ("Actions", "Actions"),
        ["Common.Status"] = ("Statut", "Status"),
        ["Common.Express"] = ("Express", "Express"),
        ["Common.Bulky"] = ("Volumineux", "Bulky"),
        ["Common.Fcfa"] = ("FCFA", "FCFA"),
        ["Common.Print"] = ("Imprimer", "Print"),
        ["Common.DarkMode"] = ("Thème sombre", "Dark mode"),

        // Home & Auth
        ["Home.WelcomeTitle"] = ("Centre opérationnel NyangoPress", "NyangoPress Operations Hub"),
        ["Home.WelcomeSubtitle"] = ("Gestion complète du dépôt, production QR, stockage, livraison GPS et enchères", "Complete management of intake, QR production, storage, GPS delivery and auctions"),
        ["Home.QuickRoles"] = ("Connexion rapide par rôle (Démo Dev 2)", "Quick role sign-in (Dev 2 Demo)"),
        ["Home.ActiveOrders"] = ("Commandes actives", "Active orders"),
        ["Home.ProductionQueue"] = ("Articles en atelier", "Items in workshop"),
        ["Home.PendingDeliveries"] = ("Livraisons en cours", "Active deliveries"),
        ["Home.AuctionLots"] = ("Lots aux enchères", "Auction lots"),

        // Customers
        ["Customers.Title"] = ("Gestion des Clients", "Customer Management"),
        ["Customers.Subtitle"] = ("Particuliers, entreprises B2B, préférences de canal, doublons et import/export", "Individuals, B2B accounts, channel preferences, duplicates and import/export"),
        ["Customers.New"] = ("Nouveau client", "New Customer"),
        ["Customers.All"] = ("Tous", "All"),
        ["Customers.Individual"] = ("Particuliers", "Individuals"),
        ["Customers.Corporate"] = ("Professionnels", "Corporate B2B"),
        ["Customers.FullName"] = ("Nom complet", "Full name"),
        ["Customers.Phone"] = ("Téléphone", "Phone"),
        ["Customers.WhatsApp"] = ("WhatsApp", "WhatsApp"),
        ["Customers.Email"] = ("E-mail", "Email"),
        ["Customers.Address"] = ("Adresse", "Address"),
        ["Customers.Notes"] = ("Notes", "Notes"),
        ["Customers.OrdersCount"] = ("Commandes", "Orders"),
        ["Customers.Balance"] = ("Solde dû", "Balance due"),
        ["Customers.Points"] = ("Points", "Points"),
        ["Customers.MergeDuplicates"] = ("Fusionner doublons", "Merge duplicates"),
        ["Customers.DuplicateWarning"] = ("Doublons potentiels détectés !", "Potential duplicates detected!"),
        ["Customers.Preferences"] = ("Préférences de contact", "Contact Preferences"),
        ["Customers.Contract"] = ("Contrat Entreprise B2B", "B2B Company Contract"),
        ["Customers.History"] = ("Historique client", "Customer History"),

        // Orders & POS
        ["Orders.Title"] = ("Commandes & Dépôts", "Orders & Deposits"),
        ["Orders.Subtitle"] = ("Suivi des tickets, retraits signés, QR codes et réclamations", "Ticket tracking, signed pickups, QR codes and claims"),
        ["Orders.New"] = ("Nouvelle commande", "New Order"),
        ["Orders.WizardTitle"] = ("Poste de Caisse — Nouveau Dépôt", "Cash Desk — New Order Wizard"),
        ["Orders.Step1"] = ("1. Client", "1. Customer"),
        ["Orders.Step2"] = ("2. Articles & Inspection", "2. Items & Inspection"),
        ["Orders.Step3"] = ("3. Livraison & Retrait", "3. Delivery & Pickup"),
        ["Orders.Step4"] = ("4. Résumé & CGV", "4. Summary & Terms"),
        ["Orders.AddItem"] = ("Ajouter l'article", "Add item"),
        ["Orders.EstimateDate"] = ("Estimer la date (Charge atelier)", "Estimate date (Workshop load)"),
        ["Orders.AcceptTerms"] = ("Le client accepte les Conditions Générales (frais de garde après 7j, enchères après 60j)", "Customer accepts Terms & Conditions (storage fees after 7d, auction after 60d)"),
        ["Orders.SubmitOrder"] = ("Valider & Générer Ticket QR", "Create Order & QR Labels"),
        ["Orders.Pickup"] = ("Valider le retrait", "Complete Pickup"),
        ["Orders.CancelOrder"] = ("Annuler la commande", "Cancel Order"),
        ["Orders.Claims"] = ("Réclamations & Litiges", "Claims & Disputes"),
        ["Orders.PrintTicket"] = ("Imprimer Ticket & Étiquettes QR", "Print Ticket & QR Labels"),

        // Production & Storage
        ["Production.Title"] = ("File de Production (Kanban)", "Production Queue (Kanban)"),
        ["Production.Subtitle"] = ("Suivi temps réel SignalR, priorité Express en tête et scan QR instantané", "Real-time SignalR tracking, Express priority first & instant QR scan"),
        ["Production.ScanQr"] = ("Scanner / Saisir QR article", "Scan / Enter Item QR"),
        ["Storage.Title"] = ("Plan de Stockage", "Storage Rack Map"),
        ["Storage.Subtitle"] = ("Allées, racks et cintres pour le rangement des articles prêts", "Aisles, racks and hangers for ready garments"),

        // Delivery & Driver
        ["Delivery.Title"] = ("Livraisons, Zones & Tournées", "Deliveries, Zones & Tours"),
        ["Delivery.Subtitle"] = ("Affectation des livreurs, tournées ordonnées, zones tarifaires et collectes en ligne", "Driver dispatch, ordered tours, tariff zones and online pickup requests"),
        ["Driver.Title"] = ("Application Livreur", "Driver Mobile App"),
        ["Driver.Subtitle"] = ("Missions du jour, partage GPS en direct et validation par code 4 chiffres", "Daily missions, live GPS sharing and 4-digit OTP proof"),

        // Public Portal
        ["Portal.Title"] = ("Suivi de Commande NyangoPress", "NyangoPress Order Tracking"),
        ["Portal.Subtitle"] = ("Consultez l'avancement de vos vêtements en temps réel ou demandez une collecte à domicile", "Track your garments in real time or request a home pickup"),
        ["Portal.TrackBtn"] = ("Suivre ma commande", "Track my order"),
        ["Portal.RequestPickup"] = ("Demander une collecte", "Request a pickup"),

        // Auctions
        ["Auctions.Title"] = ("Articles Non Réclamés & Enchères", "Unclaimed Items & Auctions"),
        ["Auctions.Subtitle"] = ("Détection automatique (>60j), frais de garde, création de lots et adjudication", "Automatic detection (>60d), storage fees, lot creation and awarding")
    });
}
