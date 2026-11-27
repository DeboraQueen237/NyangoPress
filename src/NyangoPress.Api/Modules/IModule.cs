namespace NyangoPress.Api.Modules;

/// <summary>
/// Un module = une classe qui branche ses services et ses endpoints. Program.cs découvre automatiquement
/// toutes les classes qui implémentent IModule : PERSONNE ne modifie Program.cs.
/// Pour ajouter votre module : créez <c>Endpoints/&lt;VotreModule&gt;/&lt;VotreModule&gt;Module.cs</c>.
/// </summary>
public interface IModule
{
    /// <summary>Ordre de chargement (0 = noyau, 1 = sécurité, 10 = identité, 100 = par défaut).</summary>
    int Order => 100;

    void AddServices(WebApplicationBuilder builder);

    void MapEndpoints(WebApplication app);
}
