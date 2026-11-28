namespace NyangoPress.Domain.Common;

/// <summary>
/// Base entity shared by all persistent domain entities in NyangoPress.
/// </summary>
public abstract class BaseEntity : IAuditable, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = DefaultBranchId;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token incremented on each update.
    /// </summary>
    public long RowVersion { get; set; } = 1;

    public static readonly Guid DefaultBranchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
}
