using NyangoPress.Domain.Identity;

namespace NyangoPress.Application.Common;

/// <summary>
/// Politiques d'autorisation = traduction de la matrice des droits du cahier des charges (chapitre 7).
/// Chaque endpoint déclare UNE de ces politiques : <c>.RequireAuthorization(Policies.CreateOrder)</c>.
/// Si un droit manque, ajoutez-le ici par une PR relue par Dev 1.
/// Le test AuthorizationMatrixTests vérifie automatiquement que chaque rôle non autorisé reçoit 403.
/// </summary>
public static class Policies
{
    /// <summary>Tout utilisateur connecté qui a déjà remplacé son mot de passe temporaire.</summary>
    public const string Authenticated = nameof(Authenticated);

    /// <summary>Tout utilisateur connecté, même s'il doit encore changer son mot de passe (changement, déconnexion, profil minimal).</summary>
    public const string AuthenticatedAny = nameof(AuthenticatedAny);

    public const string ManageEmployees = nameof(ManageEmployees);
    public const string AdminOnly = nameof(AdminOnly);
    public const string ViewReports = nameof(ViewReports);
    public const string OperationalDashboard = nameof(OperationalDashboard);
    public const string ManageCustomers = nameof(ManageCustomers);
    public const string CreateOrder = nameof(CreateOrder);
    public const string ChangeItemStatus = nameof(ChangeItemStatus);
    public const string TakePayment = nameof(TakePayment);
    public const string ExceptionalDiscount = nameof(ExceptionalDiscount);
    public const string ManageCatalog = nameof(ManageCatalog);
    public const string ManageStock = nameof(ManageStock);
    public const string StockOutflow = nameof(StockOutflow);
    public const string ManageSuppliers = nameof(ManageSuppliers);
    public const string ViewDeliveryMissions = nameof(ViewDeliveryMissions);
    public const string AssignDelivery = nameof(AssignDelivery);
    public const string RedeemLoyalty = nameof(RedeemLoyalty);
    public const string ViewLoyalty = nameof(ViewLoyalty);
    public const string ManageNotifications = nameof(ManageNotifications);
    public const string TrackOwnOrder = nameof(TrackOwnOrder);

    /// <summary>Rôles autorisés pour chaque politique (source unique, utilisée par l'API et par les tests).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> RolesByPolicy = new Dictionary<string, string[]>
    {
        [Authenticated] = Roles.All,
        [AuthenticatedAny] = Roles.All,

        // Gérer les employés et les rôles : ADM, MGR
        [ManageEmployees] = [Roles.Admin, Roles.Manager],

        // Paramètres, audit, sauvegardes : ADM seul
        [AdminOnly] = [Roles.Admin],

        // Tableau de bord global et rapports : ADM, MGR
        [ViewReports] = [Roles.Admin, Roles.Manager],

        // Tableau de bord opérationnel : ADM, MGR, CAI, OPE
        [OperationalDashboard] = [Roles.Admin, Roles.Manager, Roles.Cashier, Roles.Operator],

        // Clients (créer, modifier) : ADM, MGR, CAI
        [ManageCustomers] = [Roles.Admin, Roles.Manager, Roles.Cashier],

        // Créer une commande, imprimer un ticket : ADM, MGR, CAI
        [CreateOrder] = [Roles.Admin, Roles.Manager, Roles.Cashier],

        // Changer le statut d'un article : ADM, MGR, CAI, OPE
        [ChangeItemStatus] = [Roles.Admin, Roles.Manager, Roles.Cashier, Roles.Operator],

        // Encaisser, émettre une facture : ADM, MGR, CAI
        [TakePayment] = [Roles.Admin, Roles.Manager, Roles.Cashier],

        // Remise exceptionnelle, avoir, annulation : ADM, MGR
        [ExceptionalDiscount] = [Roles.Admin, Roles.Manager],

        // Catalogue et tarifs : ADM, MGR
        [ManageCatalog] = [Roles.Admin, Roles.Manager],

        // Stocks, inventaire, équipements : ADM, MGR
        [ManageStock] = [Roles.Admin, Roles.Manager],

        // Sorties de stock : ADM, MGR et OPE (seulement les sorties)
        [StockOutflow] = [Roles.Admin, Roles.Manager, Roles.Operator],

        // Fournisseurs et bons de commande : ADM, MGR
        [ManageSuppliers] = [Roles.Admin, Roles.Manager],

        // Missions de livraison (voir/exécuter) : ADM, MGR, LIV
        [ViewDeliveryMissions] = [Roles.Admin, Roles.Manager, Roles.Driver],

        // Affecter une livraison : ADM, MGR, CAI
        [AssignDelivery] = [Roles.Admin, Roles.Manager, Roles.Cashier],

        // Échanger des points en caisse : ADM, MGR, CAI
        [RedeemLoyalty] = [Roles.Admin, Roles.Manager, Roles.Cashier],

        // Consulter la fidélité : ADM, MGR, CAI, CLI
        [ViewLoyalty] = [Roles.Admin, Roles.Manager, Roles.Cashier, Roles.Customer],

        // Modèles de notifications et journal d'envoi : ADM, MGR
        [ManageNotifications] = [Roles.Admin, Roles.Manager],

        // Suivre sa commande (portail) : CLI
        [TrackOwnOrder] = [Roles.Customer],
    };
}
