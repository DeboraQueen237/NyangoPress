using NyangoPress.Domain.Common;
using NyangoPress.Domain.Orders;

namespace NyangoPress.Domain.Customers;

public class Customer : BaseEntity
{
    public string CustomerCode { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; } = CustomerType.Individual;
    public string FullName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string NormalizedPhone { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public int LoyaltyPoints { get; set; }
    public int NyangoTokens { get; set; }
    public Guid? MergedIntoCustomerId { get; set; }

    public ContactPreference ContactPreference { get; set; } = new();
    public CompanyContract? CompanyContract { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public static string NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static string NormalizePhoneDigits(string? phone)
        => string.IsNullOrWhiteSpace(phone)
            ? string.Empty
            : new string(phone.Where(char.IsDigit).ToArray());
}
