namespace NyangoPress.Domain.Common;

/// <summary>
/// Base de toutes les entités métier : identifiant, succursale, audit de création/modification
/// et suppression logique. Chaque module hérite de cette classe.
/// </summary>
public abstract class BaseEntity : IAuditable, IBranchScoped
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid BranchId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    /// <summary>Suppression logique : la ligne reste en base mais n'apparaît plus dans les requêtes.</summary>
    public bool IsDeleted { get; set; }
}
