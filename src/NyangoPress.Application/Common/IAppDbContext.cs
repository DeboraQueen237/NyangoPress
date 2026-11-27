using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace NyangoPress.Application.Common;

/// <summary>
/// Accès aux données pour la couche Application, sans dépendre de l'implémentation (PostgreSQL).
/// Le contexte réel est AppDbContext (Infrastructure). Les entités s'obtiennent avec <c>Set&lt;T&gt;()</c>.
/// </summary>
public interface IAppDbContext
{
    DatabaseFacade Database { get; }

    DbSet<TEntity> Set<TEntity>()
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
