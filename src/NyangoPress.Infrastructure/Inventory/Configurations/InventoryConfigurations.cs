using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NyangoPress.Domain.Inventory;
using NyangoPress.Domain.Suppliers;

namespace NyangoPress.Infrastructure.Inventory.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("products", t =>
            t.HasCheckConstraint("ck_products_quantity_non_negative", "\"QuantityOnHand\" >= 0"));
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Sku).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Sku).IsUnique();
        b.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        b.Property(x => x.QuantityOnHand).HasPrecision(12, 3);
        b.Property(x => x.MinThreshold).HasPrecision(12, 3);
        b.Property(x => x.ReorderQuantity).HasPrecision(12, 3);
        b.Property(x => x.UnitCost).HasPrecision(14, 2);
        b.Ignore(x => x.IsBelowThreshold);
        b.Ignore(x => x.IsOutOfStock);

        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.DefaultSupplierId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Movements).WithOne(m => m.Product).HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);

        // Verrou optimiste PostgreSQL : deux sorties simultanées ne peuvent pas rendre le stock négatif.
        // Avec Npgsql, une propriété uint marquée IsRowVersion est mappée sur la colonne système xmin.
        b.Property<uint>("Version").IsRowVersion();
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("stock_movements");
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Quantity).HasPrecision(12, 3);
        b.Property(x => x.QuantityAfter).HasPrecision(12, 3);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasIndex(x => new { x.ProductId, x.OccurredAt });
    }
}

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("suppliers");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.ContactName).HasMaxLength(150);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.WhatsApp).HasMaxLength(20);
        b.Property(x => x.Address).HasMaxLength(300);
        b.Property(x => x.Notes).HasMaxLength(1000);
    }
}

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.ToTable("purchase_orders");
        b.Property(x => x.Number).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.Number).IsUnique();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Ignore(x => x.Total);
        b.Ignore(x => x.IsFullyReceived);
        b.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> b)
    {
        b.ToTable("purchase_order_lines");
        b.Property(x => x.QuantityOrdered).HasPrecision(12, 3);
        b.Property(x => x.QuantityReceived).HasPrecision(12, 3);
        b.Property(x => x.UnitPrice).HasPrecision(14, 2);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
