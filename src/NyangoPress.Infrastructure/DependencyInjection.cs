using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NyangoPress.Application.Auctions;
using NyangoPress.Application.Common;
using NyangoPress.Application.Customers;
using NyangoPress.Application.Delivery;
using NyangoPress.Application.Orders;
using NyangoPress.Infrastructure.Auctions;
using NyangoPress.Infrastructure.Common;
using NyangoPress.Infrastructure.Customers;
using NyangoPress.Infrastructure.Delivery;
using NyangoPress.Infrastructure.Orders;
using NyangoPress.Infrastructure.Persistence;
using NyangoPress.Shared.Auctions;
using NyangoPress.Shared.Customers;
using NyangoPress.Shared.Delivery;
using NyangoPress.Shared.Orders;

namespace NyangoPress.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNyangoPressApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerValidator>();
        services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerValidator>();
        services.AddScoped<IValidator<UpsertCompanyContractRequest>, UpsertCompanyContractValidator>();
        services.AddScoped<IValidator<MergeCustomersRequest>, MergeCustomersValidator>();

        services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderValidator>();
        services.AddScoped<IValidator<PickupOrderRequest>, PickupOrderValidator>();
        services.AddScoped<IValidator<CancelOrderRequest>, CancelOrderValidator>();
        services.AddScoped<IValidator<CreateClaimRequest>, CreateClaimValidator>();
        services.AddScoped<IValidator<ResolveClaimRequest>, ResolveClaimValidator>();
        services.AddScoped<IValidator<CreateStorageLocationRequest>, CreateStorageLocationValidator>();

        services.AddScoped<IValidator<UpsertDeliveryZoneRequest>, UpsertDeliveryZoneValidator>();
        services.AddScoped<IValidator<CreatePickupRequest>, CreatePickupRequestValidator>();
        services.AddScoped<IValidator<BroadcastLocationRequest>, BroadcastLocationValidator>();

        services.AddScoped<IValidator<CreateAuctionLotRequest>, CreateAuctionLotValidator>();
        services.AddScoped<IValidator<PlaceBidRequest>, PlaceBidValidator>();

        return services;
    }

    public static IServiceCollection AddNyangoPressInfrastructure(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureDb,
        string? uploadsRootPath = null)
    {
        services.AddDbContext<AppDbContext>(configureDb);
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<INotificationService, LoggingNotificationService>();
        services.AddSingleton<IPricingService, DefaultPricingService>();
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(uploadsRootPath));
        services.AddScoped<ISequenceGenerator, SequenceGenerator>();

        services.AddCustomersModule();
        services.AddOrdersModule();
        services.AddDeliveryModule();
        services.AddAuctionsModule();

        return services;
    }

    public static IServiceCollection AddCustomersModule(this IServiceCollection services)
    {
        services.AddScoped<ICustomerStore, CustomerStore>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerImportExportService, CustomerImportExportService>();
        return services;
    }

    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddScoped<IOrderStore, OrderStore>();
        services.AddScoped<IOrderQueries, OrderQueries>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddSingleton<IQrCodeService, QrCodeService>();
        return services;
    }

    public static IServiceCollection AddDeliveryModule(this IServiceCollection services)
    {
        services.AddScoped<IDeliveryStore, DeliveryStore>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IPublicTrackingService, PublicTrackingService>();
        return services;
    }

    public static IServiceCollection AddAuctionsModule(this IServiceCollection services)
    {
        services.AddScoped<IAuctionStore, AuctionStore>();
        services.AddScoped<IAuctionService, AuctionService>();
        return services;
    }
}
