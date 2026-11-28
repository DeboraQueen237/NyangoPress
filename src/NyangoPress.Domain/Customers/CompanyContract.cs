using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Customers;

public class CompanyContract : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? RegistrationNumber { get; set; }
    public decimal NegotiatedDiscountPercent { get; set; }
    public bool MonthlyGroupBilling { get; set; }
    public decimal CreditLimitFcfa { get; set; }
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BillingContactName { get; set; }
    public string? BillingEmail { get; set; }
}
