namespace NyangoPress.Domain.Common;

/// <summary>Entité rattachée à une succursale (multi-succursales, SET-03).</summary>
public interface IBranchScoped
{
    Guid BranchId { get; set; }
}
