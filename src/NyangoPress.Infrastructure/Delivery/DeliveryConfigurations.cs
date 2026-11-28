using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NyangoPress.Domain.Delivery;

namespace NyangoPress.Infrastructure.Delivery;

public sealed class DeliveryZoneConfiguration : IEntityTypeConfiguration<DeliveryZone>
{
    public void Configure(EntityTypeBuilder<DeliveryZone> builder)
    {
        builder.ToTable("delivery_zones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Districts).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FeeFcfa).HasPrecision(18, 2);
        builder.Property(x => x.TimeSlots).HasMaxLength(250).IsRequired();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class DeliveryOrderConfiguration : IEntityTypeConfiguration<DeliveryOrder>
{
    public void Configure(EntityTypeBuilder<DeliveryOrder> builder)
    {
        builder.ToTable("delivery_orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DeliveryFeeFcfa).HasPrecision(18, 2);
        builder.Property(x => x.Address).HasMaxLength(300).IsRequired();
        builder.Property(x => x.DriverName).HasMaxLength(150);
        builder.Property(x => x.OtpCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.TimeSlot).HasMaxLength(60);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Order)
            .WithOne(o => o.DeliveryOrder)
            .HasForeignKey<DeliveryOrder>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Zone)
            .WithMany()
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tour)
            .WithMany(t => t.Stops)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class DeliveryTourConfiguration : IEntityTypeConfiguration<DeliveryTour>
{
    public void Configure(EntityTypeBuilder<DeliveryTour> builder)
    {
        builder.ToTable("delivery_tours");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TourCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DriverName).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.TourCode).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class DriverLocationConfiguration : IEntityTypeConfiguration<DriverLocation>
{
    public void Configure(EntityTypeBuilder<DriverLocation> builder)
    {
        builder.ToTable("driver_locations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DriverName).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new { x.DriverId, x.RecordedAt });
    }
}

public sealed class PickupRequestConfiguration : IEntityTypeConfiguration<PickupRequest>
{
    public void Configure(EntityTypeBuilder<PickupRequest> builder)
    {
        builder.ToTable("pickup_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReferenceCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CustomerEmail).HasMaxLength(180);
        builder.Property(x => x.Address).HasMaxLength(300).IsRequired();
        builder.Property(x => x.PreferredTimeSlot).HasMaxLength(60).IsRequired();
        builder.HasIndex(x => x.ReferenceCode).IsUnique();
    }
}
