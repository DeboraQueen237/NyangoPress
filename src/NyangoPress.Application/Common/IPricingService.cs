using NyangoPress.Shared.Orders;

namespace NyangoPress.Application.Common;

public sealed record OrderDraftLine(
    string GarmentCode,
    string ServiceCode,
    bool IsExpress,
    bool IsBulky);

public sealed record OrderDraft(
    Guid CustomerId,
    DeliveryOption DeliveryOption,
    decimal DeliveryFeeFcfa,
    decimal CustomerContractDiscountPercent,
    bool IsExpress,
    string? PromoCode,
    IReadOnlyList<OrderDraftLine> Lines);

public sealed record OrderLinePricingResult(
    string GarmentCode,
    string GarmentName,
    string ServiceCode,
    string ServiceName,
    decimal BasePriceFcfa,
    decimal SurchargeFcfa,
    decimal DiscountFcfa,
    decimal FinalPriceFcfa,
    int StandardHours);

public sealed record OrderPricingResult(
    IReadOnlyList<OrderLinePricingResult> Lines,
    decimal SubtotalFcfa,
    decimal DeliveryFeeFcfa,
    decimal DiscountFcfa,
    decimal TotalFcfa);

public interface IPricingService
{
    Task<OrderPricingResult> CalculateAsync(OrderDraft draft, CancellationToken ct = default);
    IReadOnlyList<GarmentOptionDto> GetGarments();
    IReadOnlyList<ServiceOptionDto> GetServices();
}
