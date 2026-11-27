using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Identity;

/// <summary>Profil personnel d'un employé (EMP-03) : coordonnées, langue, thème, photo.</summary>
public class EmployeeProfile : BaseEntity
{
    public Guid UserId { get; set; }

    public AppUser? User { get; set; }

    /// <summary>Chemin relatif retourné par IFileStorage (jamais une URL publique).</summary>
    public string? PhotoPath { get; set; }

    public string? WhatsAppNumber { get; set; }

    public string? Address { get; set; }

    /// <summary>"fr" ou "en".</summary>
    public string Language { get; set; } = "fr";

    /// <summary>"light", "dark" ou "system".</summary>
    public string Theme { get; set; } = "system";

    public DateOnly? HireDate { get; set; }

    public string? Notes { get; set; }
}
