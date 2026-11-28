using NyangoPress.Application.Common;
using NyangoPress.Shared.Orders;

namespace NyangoPress.Infrastructure.Common;

/// <summary>
/// Server-side pricing engine implementation covering Cameroonian pressing catalog in FCFA.
/// Compatible with Dev 3 IPricingService contract.
/// </summary>
public sealed class DefaultPricingService : IPricingService
{
    private static readonly IReadOnlyList<GarmentOptionDto> Garments =
    [
        new("SHIRT", "Chemise / Polo", "Shirt / Polo", "Hauts"),
        new("TROUSERS", "Pantalon / Jean", "Trousers / Jeans", "Bas"),
        new("SUIT", "Costume complet (2 pièces)", "Full Suit (2 pcs)", "Ensembles"),
        new("DRESS", "Robe classique", "Classic Dress", "Robes"),
        new("EVENING_DRESS", "Robe de soirée / Mariage", "Evening / Wedding Gown", "Cérémonie"),
        new("BOUBOU", "Grand Boubou / Tenue traditionnelle", "Traditional Boubou / Attire", "Traditionnel"),
        new("JACKET", "Veste / Blazer", "Jacket / Blazer", "Hauts"),
        new("DUVET", "Couette / Édredon", "Duvet / Comforter", "Maison"),
        new("CURTAIN", "Rideau (le panneau)", "Curtain (per panel)", "Maison"),
        new("LEATHER", "Article en cuir / Daim", "Leather / Suede Item", "Spécial")
    ];

    private static readonly IReadOnlyList<ServiceOptionDto> Services =
    [
        new("WASH_IRON", "Lavage + Repassage", "Wash & Iron", 24),
        new("DRY_CLEAN", "Nettoyage à sec", "Dry Cleaning", 36),
        new("IRON_ONLY", "Repassage seul", "Ironing Only", 12),
        new("STAIN_CARE", "Détachage intensif + Lavage", "Intensive Stain Removal + Wash", 48),
        new("ALTERATION", "Retouche / Couture", "Tailoring / Alteration", 48)
    ];

    private static readonly IReadOnlyDictionary<(string Garment, string Service), decimal> BasePrices =
        new Dictionary<(string, string), decimal>
        {
            [("SHIRT", "WASH_IRON")] = 1000m,
            [("SHIRT", "DRY_CLEAN")] = 1500m,
            [("SHIRT", "IRON_ONLY")] = 500m,
            [("SHIRT", "STAIN_CARE")] = 2000m,
            [("SHIRT", "ALTERATION")] = 1500m,

            [("TROUSERS", "WASH_IRON")] = 1200m,
            [("TROUSERS", "DRY_CLEAN")] = 1800m,
            [("TROUSERS", "IRON_ONLY")] = 600m,
            [("TROUSERS", "STAIN_CARE")] = 2200m,
            [("TROUSERS", "ALTERATION")] = 1500m,

            [("SUIT", "WASH_IRON")] = 3500m,
            [("SUIT", "DRY_CLEAN")] = 4500m,
            [("SUIT", "IRON_ONLY")] = 2000m,
            [("SUIT", "STAIN_CARE")] = 5500m,
            [("SUIT", "ALTERATION")] = 3500m,

            [("DRESS", "WASH_IRON")] = 2000m,
            [("DRESS", "DRY_CLEAN")] = 3000m,
            [("DRESS", "IRON_ONLY")] = 1000m,
            [("DRESS", "STAIN_CARE")] = 3500m,
            [("DRESS", "ALTERATION")] = 2500m,

            [("EVENING_DRESS", "WASH_IRON")] = 5000m,
            [("EVENING_DRESS", "DRY_CLEAN")] = 7500m,
            [("EVENING_DRESS", "IRON_ONLY")] = 3000m,
            [("EVENING_DRESS", "STAIN_CARE")] = 9000m,
            [("EVENING_DRESS", "ALTERATION")] = 5000m,

            [("BOUBOU", "WASH_IRON")] = 2500m,
            [("BOUBOU", "DRY_CLEAN")] = 3500m,
            [("BOUBOU", "IRON_ONLY")] = 1500m,
            [("BOUBOU", "STAIN_CARE")] = 4500m,
            [("BOUBOU", "ALTERATION")] = 2500m,

            [("JACKET", "WASH_IRON")] = 2000m,
            [("JACKET", "DRY_CLEAN")] = 3000m,
            [("JACKET", "IRON_ONLY")] = 1000m,
            [("JACKET", "STAIN_CARE")] = 3500m,
            [("JACKET", "ALTERATION")] = 2500m,

            [("DUVET", "WASH_IRON")] = 5000m,
            [("DUVET", "DRY_CLEAN")] = 7000m,
            [("DUVET", "IRON_ONLY")] = 2500m,
            [("DUVET", "STAIN_CARE")] = 8000m,
            [("DUVET", "ALTERATION")] = 4000m,

            [("CURTAIN", "WASH_IRON")] = 3000m,
            [("CURTAIN", "DRY_CLEAN")] = 4500m,
            [("CURTAIN", "IRON_ONLY")] = 1500m,
            [("CURTAIN", "STAIN_CARE")] = 5000m,
            [("CURTAIN", "ALTERATION")] = 3000m,

            [("LEATHER", "WASH_IRON")] = 6000m,
            [("LEATHER", "DRY_CLEAN")] = 8000m,
            [("LEATHER", "IRON_ONLY")] = 3500m,
            [("LEATHER", "STAIN_CARE")] = 9500m,
            [("LEATHER", "ALTERATION")] = 6000m
        };

    public Task<OrderPricingResult> CalculateAsync(OrderDraft draft, CancellationToken ct = default)
    {
        var garmentMap = Garments.ToDictionary(g => g.Code, StringComparer.OrdinalIgnoreCase);
        var serviceMap = Services.ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

        var promoPercent = draft.PromoCode?.Trim().ToUpperInvariant() switch
        {
            "NYANGO10" => 10m,
            "BIENVENUE15" => 15m,
            "VIP20" => 20m,
            _ => 0m
        };

        var discountPercent = Math.Min(60m, Math.Max(draft.CustomerContractDiscountPercent, promoPercent));
        var results = new List<OrderLinePricingResult>(draft.Lines.Count);

        foreach (var line in draft.Lines)
        {
            var gCode = line.GarmentCode.Trim().ToUpperInvariant();
            var sCode = line.ServiceCode.Trim().ToUpperInvariant();

            var garment = garmentMap.TryGetValue(gCode, out var g) ? g : new GarmentOptionDto(gCode, gCode, gCode, "Autre");
            var service = serviceMap.TryGetValue(sCode, out var s) ? s : new ServiceOptionDto(sCode, sCode, sCode, 24);

            var basePrice = BasePrices.TryGetValue((gCode, sCode), out var p) ? p : 1500m;

            decimal multiplier = 1m;
            if (line.IsExpress || draft.IsExpress) multiplier += 0.50m;
            if (line.IsBulky) multiplier += 0.35m;

            var gross = RoundFcfa(basePrice * multiplier);
            var surcharge = gross - basePrice;
            var discount = discountPercent > 0m ? RoundFcfa(gross * (discountPercent / 100m)) : 0m;
            var final = Math.Max(0m, gross - discount);

            results.Add(new OrderLinePricingResult(
                garment.Code,
                garment.NameFr,
                service.Code,
                service.NameFr,
                basePrice,
                surcharge,
                discount,
                final,
                service.StandardHours));
        }

        var subtotal = results.Sum(r => r.BasePriceFcfa + r.SurchargeFcfa);
        var totalDiscount = results.Sum(r => r.DiscountFcfa);
        var deliveryFee = draft.DeliveryOption == DeliveryOption.StorePickup ? 0m : Math.Max(0m, draft.DeliveryFeeFcfa);
        var total = Math.Max(0m, subtotal - totalDiscount + deliveryFee);

        return Task.FromResult(new OrderPricingResult(results, subtotal, deliveryFee, totalDiscount, total));
    }

    public IReadOnlyList<GarmentOptionDto> GetGarments() => Garments;
    public IReadOnlyList<ServiceOptionDto> GetServices() => Services;

    private static decimal RoundFcfa(decimal amount)
        => Math.Round(amount / 50m, MidpointRounding.AwayFromZero) * 50m;
}
