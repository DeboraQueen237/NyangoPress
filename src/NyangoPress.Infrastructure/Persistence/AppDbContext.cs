using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Auctions;
using NyangoPress.Domain.Common;
using NyangoPress.Domain.Customers;
using NyangoPress.Domain.Delivery;
using NyangoPress.Domain.Orders;

namespace NyangoPress.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser? currentUser = null) : DbContext(options), IUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ContactPreference> ContactPreferences => Set<ContactPreference>();
    public DbSet<CompanyContract> CompanyContracts => Set<CompanyContract>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ItemPhoto> ItemPhotos => Set<ItemPhoto>();
    public DbSet<ItemStatusHistory> ItemStatusHistory => Set<ItemStatusHistory>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<SequenceCounter> SequenceCounters => Set<SequenceCounter>();

    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DeliveryTour> DeliveryTours => Set<DeliveryTour>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<PickupRequest> PickupRequests => Set<PickupRequest>();

    public DbSet<AuctionLot> AuctionLots => Set<AuctionLot>();
    public DbSet<AuctionBid> AuctionBids => Set<AuctionBid>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configure primary key generation and optimistic concurrency token uniformly across all BaseEntity subtypes
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Id))
                    .ValueGeneratedNever();

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.RowVersion))
                    .IsConcurrencyToken();
            }
        }

        // SQLite cannot translate DateTimeOffset comparisons/ORDER BY natively; convert to UTC ticks
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var dtoConverter = new DateTimeOffsetToBinaryConverter();
            var nullableDtoConverter = new ValueConverter<DateTimeOffset?, long?>(
                v => v.HasValue ? v.Value.ToUniversalTime().Ticks : null,
                v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                    {
                        property.SetValueConverter(dtoConverter);
                    }
                    else if (property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(nullableDtoConverter);
                    }
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var actor = currentUser?.DisplayName ?? "System";
        var branchId = currentUser?.BranchId ?? BaseEntity.DefaultBranchId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.BranchId == Guid.Empty)
                        entry.Entity.BranchId = branchId;
                    if (entry.Entity.CreatedAt == default)
                        entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= actor;
                    if (entry.Entity.RowVersion <= 0)
                        entry.Entity.RowVersion = 1;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = actor;
                    entry.Entity.RowVersion++;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
