using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NyangoPress.Domain.Auctions;

namespace NyangoPress.Infrastructure.Auctions;

public sealed class AuctionLotConfiguration : IEntityTypeConfiguration<AuctionLot>
{
    public void Configure(EntityTypeBuilder<AuctionLot> builder)
    {
        builder.ToTable("auction_lots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LotNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.StartingPriceFcfa).HasPrecision(18, 2);
        builder.Property(x => x.CurrentHighestBidFcfa).HasPrecision(18, 2);
        builder.Property(x => x.MinimumIncrementFcfa).HasPrecision(18, 2);
        builder.Property(x => x.FinalSalePriceFcfa).HasPrecision(18, 2);
        builder.Property(x => x.WinningBidderName).HasMaxLength(150);
        builder.Property(x => x.WinningBidderPhone).HasMaxLength(30);
        builder.HasIndex(x => x.LotNumber).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class AuctionBidConfiguration : IEntityTypeConfiguration<AuctionBid>
{
    public void Configure(EntityTypeBuilder<AuctionBid> builder)
    {
        builder.ToTable("auction_bids");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BidderName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.BidderPhone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.AmountFcfa).HasPrecision(18, 2);
        builder.HasOne(x => x.AuctionLot)
            .WithMany(l => l.Bids)
            .HasForeignKey(x => x.AuctionLotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
