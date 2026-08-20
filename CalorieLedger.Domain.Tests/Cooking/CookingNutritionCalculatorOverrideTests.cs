using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Tests.Cooking;

public sealed class CookingNutritionCalculatorOverrideTests {
    [Fact]
    public void Calculate_WithPer100GramOverride_UsesOverrideForDishAndTotal() {
        var draft = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Блюдо",
            Ingredients: [
                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Ингредиент",
                    Quantity: FoodQuantity.Grams(100m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 50m,
                        ProteinG: 1m,
                        FatG: 1m,
                        CarbsG: 5m
                    )
                ),
            ],
            OutputWeightG: 250m,
            NutritionPer100GramsOverride: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 200m,
                ProteinG: 10m,
                FatG: 8m,
                CarbsG: 20m
            )
        );

        var result = CookingNutritionCalculator.Calculate(draft);

        Assert.Equal(200m, result.NutritionPer100Grams.CaloriesKcal);
        Assert.Equal(10m, result.NutritionPer100Grams.ProteinG);
        Assert.Equal(500m, result.TotalNutrition.CaloriesKcal);
        Assert.Equal(25m, result.TotalNutrition.ProteinG);
        Assert.Equal(20m, result.TotalNutrition.FatG);
        Assert.Equal(50m, result.TotalNutrition.CarbsG);
    }
}
