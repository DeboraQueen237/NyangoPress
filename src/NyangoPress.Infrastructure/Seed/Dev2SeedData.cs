using Microsoft.EntityFrameworkCore;
using NyangoPress.Domain.Auctions;
using NyangoPress.Domain.Customers;
using NyangoPress.Domain.Delivery;
using NyangoPress.Domain.Orders;
using NyangoPress.Infrastructure.Persistence;

namespace NyangoPress.Infrastructure.Seed;

public static class Dev2SeedData
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Customers.AnyAsync(ct))
            return;

        var now = DateTimeOffset.UtcNow;

        // 1. Storage Locations (Aisle A & B)
        var storageLocations = new List<StorageLocation>();
        foreach (var aisle in new[] { "A", "B" })
        {
            for (var r = 1; r <= 2; r++)
            {
                for (var h = 1; h <= 4; h++)
                {
                    var rack = $"R{r}";
                    var hanger = $"{h:D2}";
                    storageLocations.Add(new StorageLocation
                    {
                        Aisle = aisle,
                        Rack = rack,
                        HangerNumber = hanger,
                        Code = StorageLocation.BuildCode(aisle, rack, hanger)
                    });
                }
            }
        }
        db.StorageLocations.AddRange(storageLocations);

        // 2. Delivery Zones (Douala)
        var zoneAkwa = new DeliveryZone
        {
            Name = "Akwa Centre",
            Districts = "Akwa, Boulevard de la Liberté, Salle des Fêtes",
            FeeFcfa = 1000m,
            TimeSlots = "09:00-12:00, 14:00-17:00, 17:30-20:00",
            MinimumDelayHours = 3,
            IsActive = true,
            CenterLatitude = 4.0497,
            CenterLongitude = 9.7016
        };
        var zoneBonapriso = new DeliveryZone
        {
            Name = "Bonapriso / Hydrocarbures",
            Districts = "Bonapriso, Rue Koloko, Bali",
            FeeFcfa = 1500m,
            TimeSlots = "09:00-12:00, 14:00-18:00",
            MinimumDelayHours = 4,
            IsActive = true,
            CenterLatitude = 4.0279,
            CenterLongitude = 9.6948
        };
        var zoneBonanjo = new DeliveryZone
        {
            Name = "Bonanjo Administratif",
            Districts = "Bonanjo, Port, Joss",
            FeeFcfa = 1200m,
            TimeSlots = "08:30-12:00, 13:30-17:00",
            MinimumDelayHours = 3,
            IsActive = true,
            CenterLatitude = 4.0435,
            CenterLongitude = 9.6873
        };
        var zoneMakepe = new DeliveryZone
        {
            Name = "Makepe / Bonamoussadi",
            Districts = "Makepe, Bonamoussadi, Kotto",
            FeeFcfa = 2000m,
            TimeSlots = "10:00-13:00, 15:00-19:00",
            MinimumDelayHours = 5,
            IsActive = true,
            CenterLatitude = 4.0846,
            CenterLongitude = 9.7441
        };
        db.DeliveryZones.AddRange(zoneAkwa, zoneBonapriso, zoneBonanjo, zoneMakepe);

        // 3. Realistic Customers (Particulars & Corporate B2B)
        var c1 = new Customer
        {
            CustomerCode = "CLI-0001",
            CustomerType = CustomerType.Individual,
            FullName = "Amina Mbarga",
            NormalizedName = Customer.NormalizeText("Amina Mbarga"),
            Phone = "+237690123456",
            NormalizedPhone = "237690123456",
            WhatsAppNumber = "+237690123456",
            Email = "amina.mbarga@exemple.cm",
            Address = "Rue Koloko, Bonapriso, Douala",
            LoyaltyPoints = 450,
            NyangoTokens = 5,
            ContactPreference = new ContactPreference
            {
                AllowWhatsApp = true,
                AllowSms = true,
                AllowEmail = true,
                PreferredLanguage = PreferredLanguage.Fr,
                PrimaryChannel = ContactChannel.WhatsApp
            }
        };

        var c2 = new Customer
        {
            CustomerCode = "CLI-0002",
            CustomerType = CustomerType.Individual,
            FullName = "Samuel Eto'o Ndongo",
            NormalizedName = Customer.NormalizeText("Samuel Eto'o Ndongo"),
            Phone = "+237677889900",
            NormalizedPhone = "237677889900",
            WhatsAppNumber = "+237677889900",
            Email = "samuel.ndongo@exemple.cm",
            Address = "Makepe Bloc L, Douala",
            LoyaltyPoints = 180,
            NyangoTokens = 2,
            ContactPreference = new ContactPreference
            {
                AllowWhatsApp = true,
                AllowSms = true,
                PreferredLanguage = PreferredLanguage.En,
                PrimaryChannel = ContactChannel.WhatsApp
            }
        };

        var c3 = new Customer
        {
            CustomerCode = "CLI-0003",
            CustomerType = CustomerType.Corporate,
            FullName = "Hôtel Sawa Douala",
            NormalizedName = Customer.NormalizeText("Hôtel Sawa Douala"),
            Phone = "+237233420866",
            NormalizedPhone = "237233420866",
            WhatsAppNumber = "+237699110022",
            Email = "lingerie@hotelsawa.cm",
            Address = "488 Rue Verdun, Bonanjo, Douala",
            LoyaltyPoints = 2400,
            NyangoTokens = 28,
            ContactPreference = new ContactPreference
            {
                AllowWhatsApp = true,
                AllowEmail = true,
                PreferredLanguage = PreferredLanguage.Fr,
                PrimaryChannel = ContactChannel.Email
            },
            CompanyContract = new CompanyContract
            {
                CompanyName = "Hôtel Sawa SA",
                TaxId = "M0123456789A",
                RegistrationNumber = "RC/DLA/2020/B/104",
                NegotiatedDiscountPercent = 15m,
                MonthlyGroupBilling = true,
                CreditLimitFcfa = 500000m,
                ContractStartDate = DateOnly.FromDateTime(now.AddMonths(-6).Date),
                ContractEndDate = DateOnly.FromDateTime(now.AddMonths(18).Date),
                IsActive = true,
                BillingContactName = "Mme Tchoupo Clarisse",
                BillingEmail = "compta@hotelsawa.cm"
            }
        };

        var c4 = new Customer
        {
            CustomerCode = "CLI-0004",
            CustomerType = CustomerType.Corporate,
            FullName = "Clinique de l'Aéroport",
            NormalizedName = Customer.NormalizeText("Clinique de l'Aéroport"),
            Phone = "+237655443322",
            NormalizedPhone = "237655443322",
            Email = "intendance@clinique.cm",
            Address = "Bonapriso, Douala",
            LoyaltyPoints = 900,
            NyangoTokens = 10,
            ContactPreference = new ContactPreference
            {
                AllowEmail = true,
                AllowSms = true,
                PreferredLanguage = PreferredLanguage.Fr,
                PrimaryChannel = ContactChannel.Email
            },
            CompanyContract = new CompanyContract
            {
                CompanyName = "Clinique de l'Aéroport SARL",
                TaxId = "M0987654321Z",
                NegotiatedDiscountPercent = 10m,
                MonthlyGroupBilling = true,
                CreditLimitFcfa = 300000m,
                IsActive = true
            }
        };

        var c5 = new Customer
        {
            CustomerCode = "CLI-0005",
            CustomerType = CustomerType.Individual,
            FullName = "Brice Kamga",
            NormalizedName = Customer.NormalizeText("Brice Kamga"),
            Phone = "+237691223344",
            NormalizedPhone = "237691223344",
            WhatsAppNumber = "+237691223344",
            Address = "Akwa Nord, Douala",
            LoyaltyPoints = 50,
            ContactPreference = new ContactPreference
            {
                AllowWhatsApp = true,
                AllowSms = true,
                PreferredLanguage = PreferredLanguage.Fr
            }
        };

        db.Customers.AddRange(c1, c2, c3, c4, c5);

        // 4. Orders across full lifecycle
        var o1 = new Order
        {
            TicketNumber = "NP-2026-0001",
            TrackingCode = "NYG101",
            CustomerId = c1.Id,
            Status = OrderStatus.InProgress,
            DeliveryOption = DeliveryOption.HomeDelivery,
            DeliveryZoneId = zoneBonapriso.Id,
            DeliveryAddress = c1.Address,
            IsExpress = true,
            AcceptedTermsAndConditions = true,
            TermsAcceptedAt = now.AddHours(-10),
            TermsSummary = "Frais de garde après 7 jours ; enchères après 60 jours.",
            SubtotalFcfa = 9750m,
            DeliveryFeeFcfa = 1500m,
            TotalFcfa = 11250m,
            PaidFcfa = 5000m,
            PromisedAt = now.AddHours(6),
            EstimatedReadyAt = now.AddHours(6),
            CreatedAt = now.AddHours(-10),
            Items =
            {
                new OrderItem
                {
                    QrCode = "NP-2026-0001-01-A1B2",
                    GarmentCode = "EVENING_DRESS",
                    GarmentName = "Robe de soirée / Mariage",
                    ServiceCode = "DRY_CLEAN",
                    ServiceName = "Nettoyage à sec",
                    Status = ItemStatus.InTreatment,
                    TreatmentSubStep = TreatmentSubStep.StainRemoval,
                    IsExpress = true,
                    Color = "Or & Ivoire",
                    Brand = "Maison Couture",
                    PredefinedDefects = "Tache huile, Perles fragiles",
                    DefectNotes = "Attention aux broderies sur l'épaule gauche",
                    BasePriceFcfa = 7500m,
                    SurchargeFcfa = 3750m,
                    FinalPriceFcfa = 11250m,
                    StatusHistory =
                    {
                        new ItemStatusHistory { FromStatus = null, ToStatus = ItemStatus.Received, ChangedBy = "Caissier Demo", ChangedAt = now.AddHours(-10), Notes = "Dépôt express" },
                        new ItemStatusHistory { FromStatus = ItemStatus.Received, ToStatus = ItemStatus.Sorted, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-8) },
                        new ItemStatusHistory { FromStatus = ItemStatus.Sorted, ToStatus = ItemStatus.InTreatment, SubStep = TreatmentSubStep.StainRemoval, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-5), Notes = "Détachage doux en cours" }
                    }
                }
            }
        };

        var readyLoc1 = storageLocations[0];
        var o2Item1 = new OrderItem
        {
            QrCode = "NP-2026-0002-01-C3D4",
            GarmentCode = "SUIT",
            GarmentName = "Costume complet (2 pièces)",
            ServiceCode = "DRY_CLEAN",
            ServiceName = "Nettoyage à sec",
            Status = ItemStatus.Ready,
            Color = "Bleu marine",
            Brand = "Hugo Boss",
            BasePriceFcfa = 4500m,
            FinalPriceFcfa = 4500m,
            StorageLocationId = readyLoc1.Id,
            ReadySince = now.AddHours(-4),
            StatusHistory =
            {
                new ItemStatusHistory { FromStatus = null, ToStatus = ItemStatus.Received, ChangedBy = "Caissier Demo", ChangedAt = now.AddDays(-1) },
                new ItemStatusHistory { FromStatus = ItemStatus.Received, ToStatus = ItemStatus.Sorted, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-20) },
                new ItemStatusHistory { FromStatus = ItemStatus.Sorted, ToStatus = ItemStatus.InTreatment, SubStep = TreatmentSubStep.DryCleaning, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-14) },
                new ItemStatusHistory { FromStatus = ItemStatus.InTreatment, ToStatus = ItemStatus.QualityCheck, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-6) },
                new ItemStatusHistory { FromStatus = ItemStatus.QualityCheck, ToStatus = ItemStatus.Ready, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-4), Notes = $"Rangé en {readyLoc1.Code}" }
            }
        };
        readyLoc1.IsOccupied = true;
        readyLoc1.CurrentOrderItemId = o2Item1.Id;

        var o2 = new Order
        {
            TicketNumber = "NP-2026-0002",
            TrackingCode = "NYG202",
            CustomerId = c2.Id,
            Status = OrderStatus.Ready,
            DeliveryOption = DeliveryOption.HomeDelivery,
            DeliveryZoneId = zoneMakepe.Id,
            DeliveryAddress = c2.Address,
            AcceptedTermsAndConditions = true,
            TermsAcceptedAt = now.AddDays(-1),
            TermsSummary = "Frais de garde après 7 jours ; enchères après 60 jours.",
            SubtotalFcfa = 4500m,
            DeliveryFeeFcfa = 2000m,
            TotalFcfa = 6500m,
            PaidFcfa = 6500m,
            PromisedAt = now.AddHours(2),
            EstimatedReadyAt = now.AddHours(1),
            ReadyAt = now.AddHours(-4),
            CreatedAt = now.AddDays(-1),
            Items = { o2Item1 }
        };

        var driverId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var d2 = new DeliveryOrder
        {
            OrderId = o2.Id,
            CustomerId = c2.Id,
            ZoneId = zoneMakepe.Id,
            DeliveryFeeFcfa = zoneMakepe.FeeFcfa,
            MissionType = DeliveryMissionType.Delivery,
            Status = DeliveryStatus.EnRoute,
            DriverId = driverId,
            DriverName = "Junior Tchinda",
            Address = c2.Address ?? "Makepe Bloc L",
            DestinationLatitude = zoneMakepe.CenterLatitude,
            DestinationLongitude = zoneMakepe.CenterLongitude,
            TimeSlot = "15:00-19:00",
            AssignedAt = now.AddHours(-2),
            StartedAt = now.AddMinutes(-35),
            OtpCode = "4829",
            StopSequence = 1
        };

        var tour = new DeliveryTour
        {
            TourCode = "TRN-2026-001",
            DriverId = driverId,
            DriverName = "Junior Tchinda",
            TourDate = DateOnly.FromDateTime(now.Date),
            IsStarted = true,
            StartedAt = now.AddMinutes(-35)
        };
        d2.TourId = tour.Id;

        db.DriverLocations.Add(new DriverLocation
        {
            DriverId = driverId,
            DriverName = "Junior Tchinda",
            DeliveryOrderId = d2.Id,
            Latitude = 4.0665,
            Longitude = 9.7280,
            AccuracyMeters = 12,
            RecordedAt = now.AddMinutes(-2)
        });

        var o3 = new Order
        {
            TicketNumber = "NP-2026-0003",
            TrackingCode = "NYG303",
            CustomerId = c3.Id,
            Status = OrderStatus.InProgress,
            DeliveryOption = DeliveryOption.StorePickup,
            AcceptedTermsAndConditions = true,
            TermsAcceptedAt = now.AddHours(-5),
            SubtotalFcfa = 15000m,
            DiscountFcfa = 2250m,
            TotalFcfa = 12750m,
            PaidFcfa = 0m,
            PromisedAt = now.AddDays(1),
            EstimatedReadyAt = now.AddDays(1),
            CreatedAt = now.AddHours(-5),
            Items =
            {
                new OrderItem
                {
                    QrCode = "NP-2026-0003-01-E5F6",
                    GarmentCode = "DUVET",
                    GarmentName = "Couette / Édredon",
                    ServiceCode = "WASH_IRON",
                    ServiceName = "Lavage + Repassage",
                    Status = ItemStatus.QualityCheck,
                    IsBulky = true,
                    Color = "Blanc hôtelier",
                    BasePriceFcfa = 5000m,
                    SurchargeFcfa = 1750m,
                    DiscountFcfa = 1000m,
                    FinalPriceFcfa = 5750m,
                    StatusHistory =
                    {
                        new ItemStatusHistory { FromStatus = null, ToStatus = ItemStatus.Received, ChangedBy = "Caissier Demo", ChangedAt = now.AddHours(-5) },
                        new ItemStatusHistory { FromStatus = ItemStatus.Received, ToStatus = ItemStatus.Sorted, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-4) },
                        new ItemStatusHistory { FromStatus = ItemStatus.Sorted, ToStatus = ItemStatus.InTreatment, SubStep = TreatmentSubStep.Washing, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-3) },
                        new ItemStatusHistory { FromStatus = ItemStatus.InTreatment, ToStatus = ItemStatus.QualityCheck, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-1) }
                    }
                },
                new OrderItem
                {
                    QrCode = "NP-2026-0003-02-G7H8",
                    GarmentCode = "CURTAIN",
                    GarmentName = "Rideau (le panneau)",
                    ServiceCode = "DRY_CLEAN",
                    ServiceName = "Nettoyage à sec",
                    Status = ItemStatus.Sorted,
                    Color = "Beige doré",
                    BasePriceFcfa = 4500m,
                    DiscountFcfa = 700m,
                    FinalPriceFcfa = 3800m,
                    StatusHistory =
                    {
                        new ItemStatusHistory { FromStatus = null, ToStatus = ItemStatus.Received, ChangedBy = "Caissier Demo", ChangedAt = now.AddHours(-5) },
                        new ItemStatusHistory { FromStatus = ItemStatus.Received, ToStatus = ItemStatus.Sorted, ChangedBy = "Opérateur Demo", ChangedAt = now.AddHours(-4) }
                    }
                }
            }
        };

        // 5. Unclaimed order > 70 days old for Auction Demo (INN-01)
        var readyLoc2 = storageLocations[1];
        var unclaimedItem = new OrderItem
        {
            QrCode = "NP-2026-0004-01-U9V0",
            GarmentCode = "LEATHER",
            GarmentName = "Article en cuir / Daim",
            ServiceCode = "DRY_CLEAN",
            ServiceName = "Nettoyage à sec",
            Status = ItemStatus.Unclaimed,
            Color = "Marron cognac",
            Brand = "Zara Man",
            BasePriceFcfa = 8000m,
            FinalPriceFcfa = 8000m,
            ReadySince = now.AddDays(-72),
            StorageLocationId = readyLoc2.Id,
            StatusHistory =
            {
                new ItemStatusHistory { FromStatus = null, ToStatus = ItemStatus.Received, ChangedBy = "Caissier Demo", ChangedAt = now.AddDays(-75) },
                new ItemStatusHistory { FromStatus = ItemStatus.QualityCheck, ToStatus = ItemStatus.Ready, ChangedBy = "Opérateur Demo", ChangedAt = now.AddDays(-72) },
                new ItemStatusHistory { FromStatus = ItemStatus.Ready, ToStatus = ItemStatus.Unclaimed, ChangedBy = "Système", ChangedAt = now.AddDays(-12), Notes = "Délai de garde 60j dépassé" }
            }
        };
        readyLoc2.IsOccupied = true;
        readyLoc2.CurrentOrderItemId = unclaimedItem.Id;

        var readyOverdueItem = new OrderItem
        {
            QrCode = "NP-2026-0004-02-W1X2",
            GarmentCode = "SUIT",
            GarmentName = "Costume complet (2 pièces)",
            ServiceCode = "DRY_CLEAN",
            ServiceName = "Nettoyage à sec",
            Status = ItemStatus.Ready,
            Color = "Gris anthracite",
            Brand = "Armani",
            BasePriceFcfa = 4500m,
            FinalPriceFcfa = 4500m,
            ReadySince = now.AddDays(-68),
            StatusHistory =
            {
                new ItemStatusHistory { FromStatus = null, ToStatus = ItemStatus.Received, ChangedBy = "Caissier Demo", ChangedAt = now.AddDays(-70) },
                new ItemStatusHistory { FromStatus = ItemStatus.QualityCheck, ToStatus = ItemStatus.Ready, ChangedBy = "Opérateur Demo", ChangedAt = now.AddDays(-68) }
            }
        };

        var o4 = new Order
        {
            TicketNumber = "NP-2026-0004",
            TrackingCode = "NYG404",
            CustomerId = c5.Id,
            Status = OrderStatus.PartiallyUnclaimed,
            DeliveryOption = DeliveryOption.StorePickup,
            AcceptedTermsAndConditions = true,
            TermsAcceptedAt = now.AddDays(-75),
            TermsSummary = "Frais de garde après 7 jours ; enchères après 60 jours.",
            SubtotalFcfa = 12500m,
            StorageFeeFcfa = 13000m,
            TotalFcfa = 12500m,
            PaidFcfa = 3000m,
            PromisedAt = now.AddDays(-72),
            EstimatedReadyAt = now.AddDays(-72),
            ReadyAt = now.AddDays(-72),
            CreatedAt = now.AddDays(-75),
            Items = { unclaimedItem, readyOverdueItem }
        };

        // 6. Seed an open Auction Lot + initial bid
        var auctionLot = new AuctionLot
        {
            LotNumber = "LOT-2026-001",
            Title = "Veste en cuir & articles non réclamés #1",
            Description = "Lot d'articles nettoyés à sec et non réclamés après 60 jours et 3 relances.",
            Status = AuctionLotStatus.Open,
            StartingPriceFcfa = 10000m,
            CurrentHighestBidFcfa = 12500m,
            MinimumIncrementFcfa = 1000m,
            WinningBidderName = "Pauline Ndjock",
            WinningBidderPhone = "+237695001122",
            OpensAt = now.AddDays(-2),
            ClosesAt = now.AddDays(3),
            Items = { unclaimedItem },
            Bids =
            {
                new AuctionBid
                {
                    BidderName = "Pauline Ndjock",
                    BidderPhone = "+237695001122",
                    AmountFcfa = 12500m,
                    PlacedAt = now.AddHours(-3),
                    IsWinning = true
                }
            }
        };
        unclaimedItem.AuctionLotId = auctionLot.Id;

        // 7. Public Pickup Request sample (DEL-07)
        db.PickupRequests.Add(new PickupRequest
        {
            ReferenceCode = "COL-2026-0001",
            CustomerName = "Clarisse Fotso",
            CustomerPhone = "+237698776655",
            CustomerEmail = "clarisse@exemple.cm",
            ZoneId = zoneAkwa.Id,
            Address = "Boulevard de la Liberté, face Hôtel Akwa Palace",
            PreferredTimeSlot = "14:00-17:00",
            EstimatedItemsCount = 6,
            Notes = "3 chemises, 2 pantalons, 1 robe",
            Status = PickupRequestStatus.Pending
        });

        // 8. Initialize Sequence Counters above seeded records
        var year = now.Year;
        db.SequenceCounters.AddRange(
            new SequenceCounter { Key = "CUSTOMER_CODE", LastValue = 5 },
            new SequenceCounter { Key = $"ORDER_TICKET_{year}", LastValue = 4 },
            new SequenceCounter { Key = $"DELIVERY_TOUR_{year}", LastValue = 1 },
            new SequenceCounter { Key = $"PICKUP_REQ_{year}", LastValue = 1 },
            new SequenceCounter { Key = $"AUCTION_LOT_{year}", LastValue = 1 });

        db.Orders.AddRange(o1, o2, o3, o4);
        db.DeliveryTours.Add(tour);
        db.DeliveryOrders.Add(d2);
        db.AuctionLots.Add(auctionLot);

        await db.SaveChangesAsync(ct);
    }
}
