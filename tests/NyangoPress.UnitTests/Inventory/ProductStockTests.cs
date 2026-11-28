using NyangoPress.Domain.Inventory;
using Xunit;

namespace NyangoPress.UnitTests.Inventory;

public class ProductStockTests
{
    private static readonly Guid User = Guid.NewGuid();

    private static Product NewProduct(decimal initial = 10m)
    {
        var p = new Product { Name = "Détachant", Sku = "DET-001", Unit = "L", MinThreshold = 3m };
        if (initial > 0) p.ApplyMovement(StockMovementType.Entry, initial, User, "Stock initial");
        return p;
    }

    [Fact]
    public void Une_sortie_diminue_le_stock_et_trace_l_auteur()
    {
        var p = NewProduct(10m);

        var m = p.ApplyMovement(StockMovementType.Consumption, -4m, User, "Production");

        Assert.Equal(6m, p.QuantityOnHand);
        Assert.Equal(6m, m.QuantityAfter);
        Assert.Equal(User, m.PerformedByUserId);
    }

    [Fact]
    public void Le_stock_ne_devient_jamais_negatif()
    {
        var p = NewProduct(2m);

        Assert.Throws<InvalidOperationException>(() =>
            p.ApplyMovement(StockMovementType.Consumption, -5m, User));
        Assert.Equal(2m, p.QuantityOnHand);
    }

    [Fact]
    public void Un_mouvement_sans_auteur_est_refuse()
    {
        var p = NewProduct();

        Assert.Throws<InvalidOperationException>(() =>
            p.ApplyMovement(StockMovementType.Entry, 1m, Guid.Empty));
    }

    [Fact]
    public void Le_seuil_est_detecte()
    {
        var p = NewProduct(5m);
        Assert.False(p.IsBelowThreshold);

        p.ApplyMovement(StockMovementType.Consumption, -2m, User);

        Assert.True(p.IsBelowThreshold);
        Assert.False(p.IsOutOfStock);
    }
}
