using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NyangoPress.Domain.Common;

namespace NyangoPress.Infrastructure.Persistence;

/// <summary>
/// Configuration EF de base pour toute entité qui hérite de <see cref="BaseEntity"/> : clé, index sur la succursale
/// et filtre de suppression logique (les lignes <c>IsDeleted</c> disparaissent des requêtes).
/// Exemple dans votre module :
/// <code>
/// public sealed class OrderConfiguration : BaseEntityConfiguration&lt;Order&gt;
/// {
///     protected override void ConfigureEntity(EntityTypeBuilder&lt;Order&gt; builder)
///     {
///         builder.ToTable("Orders");
///         builder.Property(o =&gt; o.TicketNumber).HasMaxLength(30).IsRequired();
///     }
/// }
/// </code>
/// </summary>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.HasIndex(e => e.BranchId);
        builder.HasQueryFilter(e => !e.IsDeleted);

        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
