namespace NyangoPress.Domain.Common;

/// <summary>
/// Marker interface for entities audited automatically on persistence.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
