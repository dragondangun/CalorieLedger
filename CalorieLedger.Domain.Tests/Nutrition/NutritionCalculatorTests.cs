using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Tests.Nutrition;

public sealed class NutritionCalculatorTests {
    [Fact]
    public void CalculateTotal_Per100GramsAndGramQuantity_ReturnsScaledTotals() {
        var facts = new NutritionFacts(
            Basis: NutritionBasis.Per100Grams,
            CaloriesKcal: 120m,
            ProteinG: 17m,
            FatG: 5m,
            CarbsG: 3m);

        var quantity = FoodQuantity.Grams(250m);

        var total = NutritionCalculator.CalculateTotal(facts, quantity);

        Assert.Equal(300m, total.CaloriesKcal);
        Assert.Equal(42.5m, total.ProteinG);
        Assert.Equal(12.5m, total.FatG);
        Assert.Equal(7.5m, total.CarbsG);
    }

    [Fact]
    public void CalculateTotal_Per100GramsAndPieceQuantity_ReturnsEmptyTotals() {
        var facts = new NutritionFacts(
            Basis: NutritionBasis.Per100Grams,
            CaloriesKcal: 120m,
            ProteinG: 17m,
            FatG: 5m,
            CarbsG: 3m);

        var quantity = FoodQuantity.Pieces(2m);

        var total = NutritionCalculator.CalculateTotal(facts, quantity);

        Assert.Null(total.CaloriesKcal);
        Assert.Null(total.ProteinG);
        Assert.Null(total.FatG);
        Assert.Null(total.CarbsG);
    }

    [Fact]
    public void CalculateTotal_PreservesMissingNutritionValues() {
        var facts = new NutritionFacts(
            Basis: NutritionBasis.Per100Grams,
            CaloriesKcal: 100m,
            ProteinG: null,
            FatG: 4m,
            CarbsG: null);

        var quantity = FoodQuantity.Grams(200m);

        var total = NutritionCalculator.CalculateTotal(facts, quantity);

        Assert.Equal(200m, total.CaloriesKcal);
        Assert.Null(total.ProteinG);
        Assert.Equal(8m, total.FatG);
        Assert.Null(total.CarbsG);
    }
}