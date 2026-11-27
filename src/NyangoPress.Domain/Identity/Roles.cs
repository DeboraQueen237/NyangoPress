namespace NyangoPress.Domain.Identity;

/// <summary>Les 6 rôles du cahier des charges (chapitre 4). Les noms sont stockés tels quels en base.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string Operator = "Operator";
    public const string Driver = "Driver";
    public const string Customer = "Customer";

    /// <summary>Tous les rôles.</summary>
    public static readonly string[] All = [Admin, Manager, Cashier, Operator, Driver, Customer];

    /// <summary>Rôles du personnel (comptes créés par un Administrateur ou un Manager).</summary>
    public static readonly string[] Staff = [Admin, Manager, Cashier, Operator, Driver];
}
