using System.Text.Json;
using NyangoPress.Application.Common;
using NyangoPress.Infrastructure.Persistence;

namespace NyangoPress.Infrastructure.Common;

public sealed class AuditService(
    AppDbContext db,
    ICurrentUser currentUser) : IAuditService
{
    public async Task LogAsync(
        string action,
        string entityType,
        Guid entityId,
        object? before,
        object? after,
        CancellationToken ct = default)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            BranchId = currentUser.BranchId,
            UserId = currentUser.UserId,
            UserName = currentUser.DisplayName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after),
            OccurredAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}
