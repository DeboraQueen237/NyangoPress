using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Identity;

namespace NyangoPress.Infrastructure.Persistence;

/// <summary>
/// Contexte EF unique de l'application. PERSONNE ne modifie ce fichier : chaque module déclare ses entités
/// avec une classe <c>IEntityTypeConfiguration&lt;T&gt;</c> dans son propre dossier (voir BaseEntityConfiguration),
/// et le contexte les découvre automatiquement. Accès aux données : <c>db.Set&lt;Order&gt;()</c>.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IAppDbContext
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
