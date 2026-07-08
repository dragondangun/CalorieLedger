using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Tests.Cooking;

public sealed class CookingNutritionCalculatorTests {
    [Fact]
    public void Calculate_WithKnownIngredients_ReturnsTotalAndPer100GramNutrition() {
        var draft = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Chicken with potatoes",
            OutputWeightG: 780m,
            Ingredients:
            [
                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Chicken breast",
                    Quantity: FoodQuantity.Grams(500m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 110m,
                        ProteinG: 23m,
                        FatG: 2m,
                        CarbsG: 0m)),

                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Potatoes",
                    Quantity: FoodQuantity.Grams(400m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 77m,
                        ProteinG: 2m,
                        FatG: 0.1m,
                        CarbsG: 17m)),

                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Oil",
                    Quantity: FoodQuantity.Grams(20m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 899m,
                        ProteinG: 0m,
                        FatG: 99.9m,
                        CarbsG: 0m))
            ]);

        var result = CookingNutritionCalculator.Calculate(draft);

        Assert.Equal(1037.8m, result.TotalNutrition.CaloriesKcal);
        Assert.Equal(123m, result.TotalNutrition.ProteinG);
        Assert.Equal(30.38m, result.TotalNutrition.FatG);
        Assert.Equal(68m, result.TotalNutrition.CarbsG);

        Assert.Equal(133.05m, Math.Round(result.NutritionPer100Grams.CaloriesKcal!.Value, 2));
        Assert.Equal(15.77m, Math.Round(result.NutritionPer100Grams.ProteinG!.Value, 2));
        Assert.Equal(3.89m, Math.Round(result.NutritionPer100Grams.FatG!.Value, 2));
        Assert.Equal(8.72m, Math.Round(result.NutritionPer100Grams.CarbsG!.Value, 2));
    }

    [Fact]
    public void Calculate_WithMissingProteinValue_PropagatesUnknownProteinOnly() {
        var draft = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Dish with unknown protein",
            OutputWeightG: 500m,
            Ingredients:
            [
                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Known ingredient",
                    Quantity: FoodQuantity.Grams(200m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 100m,
                        ProteinG: 10m,
                        FatG: 3m,
                        CarbsG: 12m)),

                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Ingredient with unknown protein",
                    Quantity: FoodQuantity.Grams(100m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 50m,
                        ProteinG: null,
                        FatG: 1m,
                        CarbsG: 5m))
            ]);

        var result = CookingNutritionCalculator.Calculate(draft);

        Assert.Equal(250m, result.TotalNutrition.CaloriesKcal);
        Assert.Null(result.TotalNutrition.ProteinG);
        Assert.Equal(7m, result.TotalNutrition.FatG);
        Assert.Equal(29m, result.TotalNutrition.CarbsG);

        Assert.Equal(50m, result.NutritionPer100Grams.CaloriesKcal);
        Assert.Null(result.NutritionPer100Grams.ProteinG);
        Assert.Equal(1.4m, result.NutritionPer100Grams.FatG);
        Assert.Equal(5.8m, result.NutritionPer100Grams.CarbsG);
    }

    [Fact]
    public void Calculate_WithZeroOutputWeight_ThrowsArgumentException() {
        var draft = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Invalid dish",
            OutputWeightG: 0m,
            Ingredients: []);

        Assert.Throws<ArgumentException>(() =>
            CookingNutritionCalculator.Calculate(draft));
    }
}