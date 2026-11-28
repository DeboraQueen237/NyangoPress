using MudBlazor;

namespace NyangoPress.Web.Components.Pages.Pos;

public record GarmentType(string Id, string Name, string Category, string Icon, string[] Sizes);

/// <summary>
/// Données factices en attendant l'API du catalogue (CAT-01, Dev 3).
/// Aucun prix ici : le prix est calculé par le serveur (IPricingService, RG-02).
/// </summary>
public static class FakeGarments
{
    private static readonly string[] Letters = { "XS", "S", "M", "L", "XL", "XXL" };
    private static readonly string[] Short = { "S", "M", "L", "XL" };
    private static readonly string[] Bed = { "1 place", "2 places" };
    private static readonly string[] OneSize = { "Standard" };

    public const string AllCategories = "Tous";

    public static readonly GarmentType[] All =
    {
        new("tshirt", "T-shirt", "Hauts", Icons.Material.Filled.Checkroom, Letters),
        new("chemise", "Chemise", "Hauts", Icons.Material.Filled.Checkroom, Letters),
        new("pull", "Pull", "Hauts", Icons.Material.Filled.Checkroom, Letters),
        new("pantalon", "Pantalon", "Bas", Icons.Material.Filled.Checkroom, Letters),
        new("jean", "Jean", "Bas", Icons.Material.Filled.Checkroom, Letters),
        new("short", "Short", "Bas", Icons.Material.Filled.Checkroom, Letters),
        new("jupe", "Jupe", "Robes et jupes", Icons.Material.Filled.DryCleaning, Letters),
        new("robe", "Robe", "Robes et jupes", Icons.Material.Filled.DryCleaning, Letters),
        new("sousvet", "Sous-vêtements", "Sous-vêtements", Icons.Material.Filled.Checkroom, Short),
        new("costume", "Costume", "Costumes et vestes", Icons.Material.Filled.DryCleaning, Short),
        new("veste", "Veste ou manteau", "Costumes et vestes", Icons.Material.Filled.DryCleaning, Letters),
        new("tissu", "Habit en tissu (pagne, boubou)", "Tenues traditionnelles", Icons.Material.Filled.LocalLaundryService, OneSize),
        new("couette", "Couette", "Linge de maison", Icons.Material.Filled.Bed, Bed),
        new("drap", "Drap", "Linge de maison", Icons.Material.Filled.Bed, Bed),
        new("rideau", "Rideau", "Linge de maison", Icons.Material.Filled.LocalLaundryService, OneSize),
        new("chaussures", "Chaussures (paire)", "Chaussures", Icons.Material.Filled.CleaningServices, OneSize)
    };

    public static IEnumerable<string> Categories =>
        new[] { AllCategories }.Concat(All.Select(g => g.Category).Distinct());
}
