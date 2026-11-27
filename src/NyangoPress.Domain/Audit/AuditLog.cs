namespace NyangoPress.Domain.Audit;

/// <summary>
/// Ligne du journal d'audit (AUD-01) : qui, quoi, quand, adresse IP, valeurs avant/après.
/// Non modifiable : le contexte EF refuse toute modification ou suppression.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset At { get; set; }

    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string? IpAddress { get; set; }

    public Guid? BranchId { get; set; }

    /// <summary>Ex. "Employee.Created", "Auth.Login", "Setting.Updated".</summary>
    public string Action { get; set; } = string.Empty;

    public string? EntityName { get; set; }

    public string? EntityId { get; set; }

    /// <summary>JSON des valeurs avant la modification.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON des valeurs après la modification.</summary>
    public string? NewValues { get; set; }
}
