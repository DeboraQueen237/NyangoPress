using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NyangoPress.Domain.Orders;

namespace NyangoPress.Infrastructure.Orders;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TicketNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.TrackingCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.DeliveryAddress).HasMaxLength(300);
        builder.Property(x => x.TermsSummary).HasMaxLength(600);
        builder.Property(x => x.PickupSignaturePath).HasMaxLength(300);
        builder.Property(x => x.BalanceOverrideReason).HasMaxLength(500);
        builder.Property(x => x.BalanceOverrideBy).HasMaxLength(150);
        builder.Property(x => x.PromoCode).HasMaxLength(40);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.CancelledBy).HasMaxLength(150);

        builder.Property(x => x.SubtotalFcfa).HasPrecision(18, 2);
        builder.Property(x => x.DeliveryFeeFcfa).HasPrecision(18, 2);
        builder.Property(x => x.StorageFeeFcfa).HasPrecision(18, 2);
        builder.Property(x => x.DiscountFcfa).HasPrecision(18, 2);
        builder.Property(x => x.TotalFcfa).HasPrecision(18, 2);
        builder.Property(x => x.PaidFcfa).HasPrecision(18, 2);
        builder.Ignore(x => x.BalanceFcfa);

        builder.HasIndex(x => x.TicketNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CustomerId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QrCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.GarmentCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.GarmentName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ServiceCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(50);
        builder.Property(x => x.Brand).HasMaxLength(80);
        builder.Property(x => x.PredefinedDefects).HasMaxLength(300);
        builder.Property(x => x.DefectNotes).HasMaxLength(500);

        builder.Property(x => x.BasePriceFcfa).HasPrecision(18, 2);
        builder.Property(x => x.SurchargeFcfa).HasPrecision(18, 2);
        builder.Property(x => x.DiscountFcfa).HasPrecision(18, 2);
        builder.Property(x => x.FinalPriceFcfa).HasPrecision(18, 2);

        builder.HasIndex(x => x.QrCode).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.StorageLocation)
            .WithMany()
            .HasForeignKey(x => x.StorageLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AuctionLot)
            .WithMany(l => l.Items)
            .HasForeignKey(x => x.AuctionLotId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ItemPhotoConfiguration : IEntityTypeConfiguration<ItemPhoto>
{
    public void Configure(EntityTypeBuilder<ItemPhoto> builder)
    {
        builder.ToTable("order_item_photos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(300).IsRequired();
        builder.Property(x => x.PublicUrl).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Caption).HasMaxLength(250);
    }
}

public sealed class ItemStatusHistoryConfiguration : IEntityTypeConfiguration<ItemStatusHistory>
{
    public void Configure(EntityTypeBuilder<ItemStatusHistory> builder)
    {
        builder.ToTable("order_item_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChangedBy).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => x.OrderItemId);
    }
}

public sealed class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.ToTable("storage_locations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Aisle).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Rack).HasMaxLength(10).IsRequired();
        builder.Property(x => x.HangerNumber).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("order_claims");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.GoodwillGestureFcfa).HasPrecision(18, 2);
        builder.Property(x => x.OpenedBy).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ResolvedBy).HasMaxLength(150);
    }
}

public sealed class SequenceCounterConfiguration : IEntityTypeConfiguration<SequenceCounter>
{
    public void Configure(EntityTypeBuilder<SequenceCounter> builder)
    {
        builder.ToTable("sequence_counters");
        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(64);
    }
}
