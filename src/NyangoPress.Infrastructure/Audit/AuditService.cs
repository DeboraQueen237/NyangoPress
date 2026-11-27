using System.Text.Json;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Audit;
using NyangoPress.Infrastructure.Persistence;

namespace NyangoPress.Infrastructure.Audit;

public sealed class AuditService(AppDbContext db, ICurrentUser currentUser, TimeProvider clock) : IAuditService
{
    public async Task LogAsync(
        string action,
        string? entityName = null,
        string? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        Guid? userId = null,
        string? userName = null,
        CancellationToken ct = default)
    {
        db.Set<AuditLog>().Add(new AuditLog
        {
            At = clock.GetUtcNow(),
            UserId = userId ?? currentUser.UserId,
            UserName = userName ?? currentUser.UserName,
            IpAddress = currentUser.IpAddress,
            BranchId = currentUser.BranchId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
        });

        await db.SaveChangesAsync(ct);
    }
}
