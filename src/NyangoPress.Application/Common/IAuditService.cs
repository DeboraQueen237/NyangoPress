namespace NyangoPress.Application.Common;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        Guid entityId,
        object? before,
        object? after,
        CancellationToken ct = default);
}
