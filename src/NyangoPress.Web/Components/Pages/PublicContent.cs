using MudBlazor;

namespace NyangoPress.Web.Components.Pages;

/// <summary>
/// Contenu provisoire des pages publiques. À terme : textes dans les ressources FR/EN,
/// prix et services lus depuis l'API du catalogue (Dev 3).
/// </summary>
public static class PublicContent
{
    public record Service(string Icon, string Title, string Text);
    public record Step(string Label, string Text, string Color);
    public record Review(string Text, string Author);

    public static readonly Service[] Services =
    {
        new(Icons.Material.Filled.LocalLaundryService, "Lavage de vêtements", "Vos habits du quotidien, lavés avec soin."),
        new(Icons.Material.Filled.Checkroom, "Repassage", "Un repassage net, prêt à porter sur cintre."),
        new(Icons.Material.Filled.DryCleaning, "Nettoyage à sec", "Costumes, robes et tissus délicats."),
        new(Icons.Material.Filled.CleaningServices, "Nettoyage de chaussures", "Vos chaussures remises à neuf."),
        new(Icons.Material.Filled.LocalShipping, "Collecte et livraison à domicile", "Nous passons chez vous, vous ne bougez pas.")
    };

    public static readonly Step[] Steps =
    {
        new("Commande", "Vous passez votre commande.", "#64748B"),
        new("Collecte", "Nous récupérons vos vêtements.", "#0E7490"),
        new("Nettoyage", "Nous les nettoyons.", "#19B5C6"),
        new("Livraison", "Nous vous les livrons.", "#16A34A")
    };

    // Exemples pour la démonstration : à remplacer par de vrais avis.
    public static readonly Review[] Reviews =
    {
        new("Mes chemises sont revenues impeccables, et j'ai suivi chaque étape depuis mon téléphone.", "Cliente, exemple"),
        new("La livraison à domicile m'a fait gagner un temps précieux.", "Client, exemple"),
        new("Les photos à la réception m'ont rassuré sur l'état de mon costume.", "Client, exemple")
    };
}
