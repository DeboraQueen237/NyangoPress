using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Audit;
using NyangoPress.Domain.Common;

namespace NyangoPress.Infrastructure.Audit;

/// <summary>
/// Intercepte chaque SaveChanges : renseigne CreatedAt/By, UpdatedAt/By, la succursale, et écrit une ligne
/// d'audit (avant/après) pour chaque entité auditable créée, modifiée ou supprimée. Refuse toute modification
/// du journal d'audit lui-même.
/// </summary>
public sealed class AuditInterceptor(ICurrentUser currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    // Jamais écrits dans le journal : secrets et champs techniques bruyants.
    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.Ordinal)
    {
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "AccessFailedCount", "LastLoginAt",
        "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy",
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();

        var now = clock.GetUtcNow();
        var userId = currentUser.UserId;
        var logs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            if (entry.Entity is AuditLog)
            {
                if (entry.State is EntityState.Modified or EntityState.Deleted)
                {
                    throw new InvalidOperationException("Le journal d'audit est immuable : modification et suppression interdites.");
                }

                continue;
            }

            if (entry.Entity is not IAuditable)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.CreatedBy)).CurrentValue = userId;

                if (entry.Entity is IBranchScoped { BranchId: var branchId } && branchId == Guid.Empty)
                {
                    entry.Property(nameof(IBranchScoped.BranchId)).CurrentValue = currentUser.BranchId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.UpdatedBy)).CurrentValue = userId;
            }

            var log = BuildLog(entry, now);
            if (log is not null)
            {
                logs.Add(log);
            }
        }

        if (logs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(logs);
        }
    }

    private AuditLog? BuildLog(EntityEntry entry, DateTimeOffset now)
    {
        var entityName = entry.Metadata.ClrType.Name;
        var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();

        Dictionary<string, object?>? oldValues = null;
        Dictionary<string, object?>? newValues = null;
        string action;

        switch (entry.State)
        {
            case EntityState.Added:
                action = entityName + ".Created";
                newValues = entry.Properties
                    .Where(p => !IgnoredProperties.Contains(p.Metadata.Name))
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                break;

            case EntityState.Deleted:
                action = entityName + ".Deleted";
                oldValues = entry.Properties
                    .Where(p => !IgnoredProperties.Contains(p.Metadata.Name))
                    .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                break;

            case EntityState.Modified:
                var changed = entry.Properties
                    .Where(p => p.IsModified
                                && !IgnoredProperties.Contains(p.Metadata.Name)
                                && !Equals(p.OriginalValue, p.CurrentValue))
                    .ToList();

                if (changed.Count == 0)
                {
                    return null;
                }

                oldValues = changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                newValues = changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

                var softDeleted = changed.Any(p => p.Metadata.Name == nameof(BaseEntity.IsDeleted) && p.CurrentValue is true);
                action = entityName + (softDeleted ? ".SoftDeleted" : ".Updated");
                break;

            default:
                return null;
        }

        return new AuditLog
        {
            At = now,
            UserId = currentUser.UserId,
            UserName = currentUser.UserName,
            IpAddress = currentUser.IpAddress,
            BranchId = currentUser.BranchId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
        };
    }
}
