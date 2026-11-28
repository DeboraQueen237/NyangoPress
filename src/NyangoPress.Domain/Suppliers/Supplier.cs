using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Suppliers;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    /// <summary>Format E.164, ex. +2376XXXXXXXX.</summary>
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Address { get; set; }
    /// <summary>Délai de livraison habituel en jours (sert aux suggestions de réappro).</summary>
    public int LeadTimeDays { get; set; } = 3;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
