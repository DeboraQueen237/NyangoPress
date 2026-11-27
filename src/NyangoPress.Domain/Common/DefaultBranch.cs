namespace NyangoPress.Domain.Common;

/// <summary>Identifiant fixe de la succursale par défaut (la même sur tous les postes de développement).</summary>
public static class DefaultBranch
{
    public static readonly Guid Id = new("00000000-0000-0000-0000-000000000001");

    public const string Code = "MAIN";

    public const string Name = "Succursale principale";
}
