using NyangoPress.Domain.Auctions;
using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Orders;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public string QrCode { get; set; } = string.Empty;
    public string GarmentCode { get; set; } = string.Empty;
    public string GarmentName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;

    public ItemStatus Status { get; set; } = ItemStatus.Received;
    public TreatmentSubStep TreatmentSubStep { get; set; } = TreatmentSubStep.None;
    public bool IsExpress { get; set; }
    public bool IsBulky { get; set; }

    public string? Color { get; set; }
    public string? Brand { get; set; }
    public string? PredefinedDefects { get; set; }
    public string? DefectNotes { get; set; }

    public decimal BasePriceFcfa { get; set; }
    public decimal SurchargeFcfa { get; set; }
    public decimal DiscountFcfa { get; set; }
    public decimal FinalPriceFcfa { get; set; }

    public Guid? StorageLocationId { get; set; }
    public StorageLocation? StorageLocation { get; set; }
    public DateTimeOffset? ReadySince { get; set; }

    public Guid? AuctionLotId { get; set; }
    public AuctionLot? AuctionLot { get; set; }

    public ICollection<ItemPhoto> Photos { get; set; } = new List<ItemPhoto>();
    public ICollection<ItemStatusHistory> StatusHistory { get; set; } = new List<ItemStatusHistory>();

    public IReadOnlyList<string> GetDefectTags()
        => string.IsNullOrWhiteSpace(PredefinedDefects)
            ? []
            : PredefinedDefects.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public void SetDefectTags(IEnumerable<string>? tags)
        => PredefinedDefects = tags is null
            ? null
            : string.Join(", ", tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
}
