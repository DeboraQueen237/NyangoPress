using DomainAuctions = NyangoPress.Domain.Auctions;
using DomainCustomers = NyangoPress.Domain.Customers;
using DomainDelivery = NyangoPress.Domain.Delivery;
using DomainOrders = NyangoPress.Domain.Orders;
using SharedAuctions = NyangoPress.Shared.Auctions;
using SharedCustomers = NyangoPress.Shared.Customers;
using SharedDelivery = NyangoPress.Shared.Delivery;
using SharedOrders = NyangoPress.Shared.Orders;

namespace NyangoPress.Application.Common;

public static class EnumMappingExtensions
{
    public static SharedCustomers.CustomerType ToShared(this DomainCustomers.CustomerType v) => (SharedCustomers.CustomerType)(int)v;
    public static DomainCustomers.CustomerType ToDomain(this SharedCustomers.CustomerType v) => (DomainCustomers.CustomerType)(int)v;

    public static SharedCustomers.PreferredLanguage ToShared(this DomainCustomers.PreferredLanguage v) => (SharedCustomers.PreferredLanguage)(int)v;
    public static DomainCustomers.PreferredLanguage ToDomain(this SharedCustomers.PreferredLanguage v) => (DomainCustomers.PreferredLanguage)(int)v;

    public static SharedCustomers.ContactChannel ToShared(this DomainCustomers.ContactChannel v) => (SharedCustomers.ContactChannel)(int)v;
    public static DomainCustomers.ContactChannel ToDomain(this SharedCustomers.ContactChannel v) => (DomainCustomers.ContactChannel)(int)v;

    public static SharedOrders.ItemStatus ToShared(this DomainOrders.ItemStatus v) => (SharedOrders.ItemStatus)(int)v;
    public static DomainOrders.ItemStatus ToDomain(this SharedOrders.ItemStatus v) => (DomainOrders.ItemStatus)(int)v;

    public static SharedOrders.TreatmentSubStep ToShared(this DomainOrders.TreatmentSubStep v) => (SharedOrders.TreatmentSubStep)(int)v;
    public static DomainOrders.TreatmentSubStep ToDomain(this SharedOrders.TreatmentSubStep v) => (DomainOrders.TreatmentSubStep)(int)v;

    public static SharedOrders.OrderStatus ToShared(this DomainOrders.OrderStatus v) => (SharedOrders.OrderStatus)(int)v;
    public static DomainOrders.OrderStatus ToDomain(this SharedOrders.OrderStatus v) => (DomainOrders.OrderStatus)(int)v;

    public static SharedOrders.DeliveryOption ToShared(this DomainOrders.DeliveryOption v) => (SharedOrders.DeliveryOption)(int)v;
    public static DomainOrders.DeliveryOption ToDomain(this SharedOrders.DeliveryOption v) => (DomainOrders.DeliveryOption)(int)v;

    public static SharedOrders.ClaimStatus ToShared(this DomainOrders.ClaimStatus v) => (SharedOrders.ClaimStatus)(int)v;
    public static DomainOrders.ClaimStatus ToDomain(this SharedOrders.ClaimStatus v) => (DomainOrders.ClaimStatus)(int)v;

    public static SharedDelivery.DeliveryMissionType ToShared(this DomainDelivery.DeliveryMissionType v) => (SharedDelivery.DeliveryMissionType)(int)v;
    public static DomainDelivery.DeliveryMissionType ToDomain(this SharedDelivery.DeliveryMissionType v) => (DomainDelivery.DeliveryMissionType)(int)v;

    public static SharedDelivery.DeliveryStatus ToShared(this DomainDelivery.DeliveryStatus v) => (SharedDelivery.DeliveryStatus)(int)v;
    public static DomainDelivery.DeliveryStatus ToDomain(this SharedDelivery.DeliveryStatus v) => (DomainDelivery.DeliveryStatus)(int)v;

    public static SharedDelivery.PickupRequestStatus ToShared(this DomainDelivery.PickupRequestStatus v) => (SharedDelivery.PickupRequestStatus)(int)v;
    public static DomainDelivery.PickupRequestStatus ToDomain(this SharedDelivery.PickupRequestStatus v) => (DomainDelivery.PickupRequestStatus)(int)v;

    public static SharedAuctions.AuctionLotStatus ToShared(this DomainAuctions.AuctionLotStatus v) => (SharedAuctions.AuctionLotStatus)(int)v;
    public static DomainAuctions.AuctionLotStatus ToDomain(this SharedAuctions.AuctionLotStatus v) => (DomainAuctions.AuctionLotStatus)(int)v;
}
