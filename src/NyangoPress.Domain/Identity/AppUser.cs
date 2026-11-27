using Microsoft.AspNetCore.Identity;
using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Identity;

/// <summary>
/// Compte utilisateur (employé). Le hachage du mot de passe, le verrouillage, la 2FA et les rôles
/// sont gérés par ASP.NET Core Identity (PBKDF2 avec sel unique).
/// </summary>
public class AppUser : IdentityUser<Guid>, IAuditable, IBranchScoped
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Un compte désactivé ne peut plus se connecter mais son historique reste (RG-11).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Vrai tant que l'employé n'a pas remplacé son mot de passe temporaire (AUTH-04).</summary>
    public bool MustChangePassword { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public Guid BranchId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public EmployeeProfile? Profile { get; set; }
}
