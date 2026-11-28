using NyangoPress.Domain.Common;

namespace NyangoPress.Domain.Customers;

public class ContactPreference : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public bool AllowWhatsApp { get; set; } = true;
    public bool AllowSms { get; set; } = true;
    public bool AllowEmail { get; set; }
    public bool MarketingConsent { get; set; } = true;
    public bool UnsubscribedAll { get; set; }
    public PreferredLanguage PreferredLanguage { get; set; } = PreferredLanguage.Fr;
    public ContactChannel PrimaryChannel { get; set; } = ContactChannel.WhatsApp;
}
