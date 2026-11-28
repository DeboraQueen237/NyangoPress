namespace NyangoPress.Shared.Customers;

public enum CustomerType
{
    Individual = 0,
    Corporate = 1
}

public enum PreferredLanguage
{
    Fr = 0,
    En = 1
}

public enum ContactChannel
{
    WhatsApp = 0,
    Sms = 1,
    Email = 2
}

public sealed record ContactPreferenceDto(
    bool AllowWhatsApp,
    bool AllowSms,
    bool AllowEmail,
    bool MarketingConsent,
    bool UnsubscribedAll,
    PreferredLanguage PreferredLanguage,
    ContactChannel PrimaryChannel);

public sealed record CompanyContractDto(
    Guid Id,
    string CompanyName,
    string? TaxId,
    string? RegistrationNumber,
    decimal NegotiatedDiscountPercent,
    bool MonthlyGroupBilling,
    decimal CreditLimitFcfa,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    bool IsActive,
    string? BillingContactName,
    string? BillingEmail);

public sealed record CustomerSummaryDto(
    Guid Id,
    string CustomerCode,
    CustomerType CustomerType,
    string FullName,
    string Phone,
    string? WhatsAppNumber,
    string? Email,
    PreferredLanguage PreferredLanguage,
    int OrdersCount,
    decimal OutstandingBalanceFcfa,
    int LoyaltyPoints,
    DateTimeOffset CreatedAt,
    long RowVersion);

public sealed record CustomerDto(
    Guid Id,
    string CustomerCode,
    CustomerType CustomerType,
    string FullName,
    string Phone,
    string? WhatsAppNumber,
    string? Email,
    string? Address,
    string? Notes,
    int LoyaltyPoints,
    int NyangoTokens,
    ContactPreferenceDto ContactPreference,
    CompanyContractDto? CompanyContract,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    long RowVersion);

public sealed record CustomerOrderHistoryItemDto(
    Guid OrderId,
    string TicketNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset PromisedAt,
    string Status,
    decimal TotalFcfa,
    decimal PaidFcfa,
    decimal BalanceFcfa,
    int ItemsCount);

public sealed record CustomerClaimHistoryItemDto(
    Guid ClaimId,
    Guid OrderId,
    string TicketNumber,
    string Title,
    string Status,
    decimal? GoodwillAmountFcfa,
    DateTimeOffset CreatedAt);

public sealed record CustomerHistoryDto(
    CustomerDto Customer,
    IReadOnlyList<CustomerOrderHistoryItemDto> Orders,
    IReadOnlyList<CustomerClaimHistoryItemDto> Claims,
    decimal TotalSpentFcfa,
    decimal TotalOutstandingFcfa);

public sealed record DuplicateMatchDto(
    Guid CustomerId,
    string CustomerCode,
    string FullName,
    string Phone,
    string? Email,
    string MatchReason,
    int ConfidenceScore);

public sealed record CreateCustomerRequest(
    CustomerType CustomerType,
    string FullName,
    string Phone,
    string? WhatsAppNumber,
    string? Email,
    string? Address,
    string? Notes,
    PreferredLanguage PreferredLanguage = PreferredLanguage.Fr,
    bool AllowWhatsApp = true,
    bool AllowSms = true,
    bool AllowEmail = false,
    bool MarketingConsent = true,
    bool IgnoreDuplicateWarning = false);

public sealed record UpdateCustomerRequest(
    CustomerType CustomerType,
    string FullName,
    string Phone,
    string? WhatsAppNumber,
    string? Email,
    string? Address,
    string? Notes,
    long RowVersion);

public sealed record UpdateContactPreferenceRequest(
    bool AllowWhatsApp,
    bool AllowSms,
    bool AllowEmail,
    bool MarketingConsent,
    bool UnsubscribedAll,
    PreferredLanguage PreferredLanguage,
    ContactChannel PrimaryChannel);

public sealed record UpsertCompanyContractRequest(
    string CompanyName,
    string? TaxId,
    string? RegistrationNumber,
    decimal NegotiatedDiscountPercent,
    bool MonthlyGroupBilling,
    decimal CreditLimitFcfa,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    bool IsActive,
    string? BillingContactName,
    string? BillingEmail);

public sealed record MergeCustomersRequest(
    Guid PrimaryCustomerId,
    Guid DuplicateCustomerId);

public sealed record CustomerQueryParams(
    string? Search = null,
    CustomerType? Type = null,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "name",
    bool SortDesc = false);

public sealed record CustomerImportErrorDto(
    int RowNumber,
    string? RawIdentifier,
    string ErrorCode,
    string Message);

public sealed record CustomerImportResultDto(
    int TotalRows,
    int ImportedCount,
    int SkippedCount,
    IReadOnlyList<CustomerImportErrorDto> Errors);
