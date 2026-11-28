using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Orders;

public class StorageLocation : BaseEntity
{
    public string Aisle { get; set; } = string.Empty;
    public string Rack { get; set; } = string.Empty;
    public string HangerNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public Guid? CurrentOrderItemId { get; set; }

    public static string BuildCode(string aisle, string rack, string hangerNumber)
        => $"{aisle.Trim().ToUpperInvariant()}-{rack.Trim().ToUpperInvariant()}-{hangerNumber.Trim().ToUpperInvariant()}";
}
