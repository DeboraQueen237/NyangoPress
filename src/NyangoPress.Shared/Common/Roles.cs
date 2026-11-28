namespace NyangoPress.Shared.Common;

/// <summary>
/// Role codes and authorization policy constants for NyangoPress.
/// </summary>
public static class Roles
{
    public const string Admin = "ADM";
    public const string Manager = "MGR";
    public const string Cashier = "CAI";
    public const string Operator = "OPE";
    public const string Driver = "LIV";
    public const string Customer = "CLI";

    public static readonly string[] AllStaff = [Admin, Manager, Cashier, Operator, Driver];
}

public static class Policies
{
    public const string StaffOnly = "StaffOnly";
    public const string CustomersManage = "CustomersManage";
    public const string CustomersAdmin = "CustomersAdmin";
    public const string OrdersCreate = "OrdersCreate";
    public const string OrdersCancel = "OrdersCancel";
    public const string OrdersOverrideBalance = "OrdersOverrideBalance";
    public const string ProductionAdvance = "ProductionAdvance";
    public const string DeliveryManage = "DeliveryManage";
    public const string DeliveryZones = "DeliveryZones";
    public const string DeliveryDriver = "DeliveryDriver";
    public const string AuctionsManage = "AuctionsManage";
}
