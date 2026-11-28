namespace NyangoPress.Domain.Common;

/// <summary>
/// Marks entities partitioned by pressing branch (succursale).
/// </summary>
public interface IBranchScoped
{
    Guid BranchId { get; set; }
}
