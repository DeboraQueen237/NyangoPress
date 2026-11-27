namespace NyangoPress.Domain.Common;

/// <summary>Entité dont la création et la modification sont horodatées et attribuées (rempli automatiquement).</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }

    Guid? CreatedBy { get; set; }

    DateTimeOffset? UpdatedAt { get; set; }

    Guid? UpdatedBy { get; set; }
}
