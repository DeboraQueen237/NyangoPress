using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NyangoPress.Domain.Customers;

namespace NyangoPress.Infrastructure.Customers;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NormalizedPhone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.WhatsAppNumber).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(180);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.CustomerCode).IsUnique();
        builder.HasIndex(x => x.NormalizedPhone);
        builder.HasIndex(x => x.NormalizedName);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.ContactPreference)
            .WithOne(p => p.Customer)
            .HasForeignKey<ContactPreference>(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CompanyContract)
            .WithOne(c => c.Customer)
            .HasForeignKey<CompanyContract>(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContactPreferenceConfiguration : IEntityTypeConfiguration<ContactPreference>
{
    public void Configure(EntityTypeBuilder<ContactPreference> builder)
    {
        builder.ToTable("customer_contact_preferences");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CustomerId).IsUnique();
    }
}

public sealed class CompanyContractConfiguration : IEntityTypeConfiguration<CompanyContract>
{
    public void Configure(EntityTypeBuilder<CompanyContract> builder)
    {
        builder.ToTable("company_contracts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TaxId).HasMaxLength(60);
        builder.Property(x => x.RegistrationNumber).HasMaxLength(80);
        builder.Property(x => x.NegotiatedDiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.CreditLimitFcfa).HasPrecision(18, 2);
        builder.Property(x => x.BillingContactName).HasMaxLength(150);
        builder.Property(x => x.BillingEmail).HasMaxLength(180);
        builder.HasIndex(x => x.CustomerId).IsUnique();
    }
}
