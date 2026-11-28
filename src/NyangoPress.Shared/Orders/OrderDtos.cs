namespace NyangoPress.Shared.Orders;

public enum ItemStatus
{
    Received = 0,
    Sorted = 1,
    InTreatment = 2,
    QualityCheck = 3,
    Ready = 4,
    Delivered = 5,
    Unclaimed = 6,
    Auctioned = 7,
    Cancelled = 8
}

public enum TreatmentSubStep
{
    None = 0,
    StainRemoval = 1,
    Washing = 2,
    DryCleaning = 3,
    Drying = 4,
    Pressing = 5,
    Finishing = 6
}

public enum OrderStatus
{
    Received = 0,
    InProgress = 1,
    PartiallyReady = 2,
    Ready = 3,
    Delivered = 4,
    PartiallyUnclaimed = 5,
    Unclaimed = 6,
    Cancelled = 7
}

public enum DeliveryOption
{
    StorePickup = 0,
    HomeDelivery = 1,
    CollectAndDeliver = 2
}

public enum ClaimStatus
{
    Open = 0,
    InReview = 1,
    Resolved = 2,
    Rejected = 3
}

public sealed record StorageLocationDto(
    Guid Id,
    string Aisle,
    string Rack,
    string HangerNumber,
    string Code,
    bool IsOccupied,
    Guid? CurrentOrderItemId,
    string? CurrentTicketNumber,
    string? CurrentItemLabel);

public sealed record ItemPhotoDto(
    Guid Id,
    string FileName,
    string Url,
    string? Caption,
    DateTimeOffset CapturedAt);

public sealed record ItemStatusHistoryDto(
    Guid Id,
    ItemStatus? FromStatus,
    ItemStatus ToStatus,
    TreatmentSubStep SubStep,
    string ChangedBy,
    DateTimeOffset ChangedAt,
    string? Notes);

public sealed record OrderItemDto(
    Guid Id,
    Guid OrderId,
    string QrCode,
    string? QrSvg,
    string GarmentCode,
    string GarmentName,
    string ServiceCode,
    string ServiceName,
    ItemStatus Status,
    TreatmentSubStep TreatmentSubStep,
    bool IsExpress,
    bool IsBulky,
    string? Color,
    string? Brand,
    IReadOnlyList<string> PredefinedDefects,
    string? DefectNotes,
    decimal BasePriceFcfa,
    decimal SurchargeFcfa,
    decimal DiscountFcfa,
    decimal FinalPriceFcfa,
    Guid? StorageLocationId,
    string? StorageLocationCode,
    IReadOnlyList<ItemPhotoDto> Photos,
    IReadOnlyList<ItemStatusHistoryDto> History,
    long RowVersion);

public sealed record ClaimDto(
    Guid Id,
    Guid OrderId,
    Guid? OrderItemId,
    string Title,
    string Description,
    ClaimStatus Status,
    string? PhotoUrl,
    string? ResolutionNotes,
    decimal? GoodwillGestureFcfa,
    string OpenedBy,
    DateTimeOffset OpenedAt,
    string? ResolvedBy,
    DateTimeOffset? ResolvedAt);

public sealed record OrderSummaryDto(
    Guid Id,
    string TicketNumber,
    string TrackingCode,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    OrderStatus Status,
    DeliveryOption DeliveryOption,
    bool IsExpress,
    int ItemsCount,
    int ReadyItemsCount,
    decimal TotalFcfa,
    decimal PaidFcfa,
    decimal BalanceFcfa,
    DateTimeOffset PromisedAt,
    DateTimeOffset CreatedAt,
    long RowVersion);

public sealed record OrderDto(
    Guid Id,
    string TicketNumber,
    string TrackingCode,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    OrderStatus Status,
    DeliveryOption DeliveryOption,
    Guid? DeliveryZoneId,
    string? DeliveryAddress,
    bool IsExpress,
    bool AcceptedTermsAndConditions,
    DateTimeOffset? TermsAcceptedAt,
    string? TermsSummary,
    decimal SubtotalFcfa,
    decimal DeliveryFeeFcfa,
    decimal StorageFeeFcfa,
    decimal DiscountFcfa,
    decimal TotalFcfa,
    decimal PaidFcfa,
    decimal BalanceFcfa,
    DateTimeOffset PromisedAt,
    DateTimeOffset EstimatedReadyAt,
    DateTimeOffset? ReadyAt,
    DateTimeOffset? PickedUpAt,
    string? PickupSignatureUrl,
    bool BalanceOverrideUsed,
    string? BalanceOverrideReason,
    string? Notes,
    string? CancellationReason,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<ClaimDto> Claims,
    DateTimeOffset CreatedAt,
    long RowVersion);

public sealed record ProductionItemCardDto(
    Guid ItemId,
    Guid OrderId,
    string TicketNumber,
    string QrCode,
    string CustomerName,
    string GarmentName,
    string ServiceName,
    ItemStatus Status,
    TreatmentSubStep SubStep,
    bool IsExpress,
    bool IsBulky,
    string? Color,
    string? Brand,
    string? DefectNotes,
    string? StorageLocationCode,
    DateTimeOffset PromisedAt,
    int PhotosCount,
    long RowVersion);

public sealed record CreateOrderItemRequest(
    string GarmentCode,
    string ServiceCode,
    bool IsExpress,
    bool IsBulky,
    string? Color,
    string? Brand,
    IReadOnlyList<string>? PredefinedDefects,
    string? DefectNotes,
    IReadOnlyList<string>? PhotoDataUrls);

public sealed record CreateOrderRequest(
    Guid CustomerId,
    DeliveryOption DeliveryOption,
    Guid? DeliveryZoneId,
    string? DeliveryAddress,
    bool IsExpress,
    DateTimeOffset? PromisedAt,
    decimal InitialDepositFcfa,
    string? PromoCode,
    bool AcceptedTermsAndConditions,
    string? Notes,
    IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record OrderPricePreviewRequest(
    Guid CustomerId,
    DeliveryOption DeliveryOption,
    Guid? DeliveryZoneId,
    bool IsExpress,
    string? PromoCode,
    IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record OrderPriceLineDto(
    string GarmentCode,
    string GarmentName,
    string ServiceCode,
    string ServiceName,
    decimal BasePriceFcfa,
    decimal SurchargeFcfa,
    decimal DiscountFcfa,
    decimal FinalPriceFcfa);

public sealed record OrderPricePreviewDto(
    IReadOnlyList<OrderPriceLineDto> Lines,
    decimal SubtotalFcfa,
    decimal DeliveryFeeFcfa,
    decimal DiscountFcfa,
    decimal TotalFcfa,
    DateTimeOffset EstimatedReadyAt);

public sealed record UpdateOrderItemInspectionRequest(
    string? Color,
    string? Brand,
    IReadOnlyList<string>? PredefinedDefects,
    string? DefectNotes,
    long RowVersion);

public sealed record AddItemPhotoRequest(
    string Base64DataUrl,
    string? Caption);

public sealed record AdvanceItemStatusRequest(
    ItemStatus TargetStatus,
    TreatmentSubStep SubStep = TreatmentSubStep.None,
    string? Notes = null,
    Guid? StorageLocationId = null,
    long? RowVersion = null);

public sealed record QrScanRequest(
    string QrCode,
    ItemStatus? TargetStatus = null,
    TreatmentSubStep SubStep = TreatmentSubStep.None,
    Guid? StorageLocationId = null,
    string? Notes = null);

public sealed record QrScanResultDto(
    bool IsSuccess,
    OrderItemDto? Item,
    string TicketNumber,
    string CustomerName,
    ItemStatus PreviousStatus,
    ItemStatus CurrentStatus,
    IReadOnlyList<ItemStatus> AllowedNextStatuses);

public sealed record AssignStorageRequest(
    Guid StorageLocationId);

public sealed record CreateStorageLocationRequest(
    string Aisle,
    string Rack,
    string HangerNumber);

public sealed record PickupOrderRequest(
    decimal PaymentCollectedFcfa,
    string? SignatureDataUrl,
    bool OverrideUnpaidBalance = false,
    string? OverrideReason = null,
    long? RowVersion = null);

public sealed record CancelOrderRequest(
    string Reason,
    long? RowVersion = null);

public sealed record CreateClaimRequest(
    Guid? OrderItemId,
    string Title,
    string Description,
    string? PhotoDataUrl);

public sealed record ResolveClaimRequest(
    ClaimStatus Status,
    string ResolutionNotes,
    decimal? GoodwillGestureFcfa);

public sealed record OrderQueryParams(
    string? Search = null,
    OrderStatus? Status = null,
    bool? ExpressOnly = null,
    Guid? CustomerId = null,
    int Page = 1,
    int PageSize = 20);

public sealed record EstimatePickupDateRequest(
    IReadOnlyList<string> ServiceCodes,
    bool IsExpress,
    DeliveryOption DeliveryOption);

public sealed record EstimatePickupDateResponse(
    DateTimeOffset EstimatedReadyAt,
    int ActiveWorkshopLoadItems,
    int EstimatedTurnaroundHours,
    string ExplanationCode);

public sealed record GarmentOptionDto(
    string Code,
    string NameFr,
    string NameEn,
    string Category);

public sealed record ServiceOptionDto(
    string Code,
    string NameFr,
    string NameEn,
    int StandardHours);
