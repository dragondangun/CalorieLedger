using CalorieLedger.Domain.Common;

namespace CalorieLedger.Domain.Nutrition;

public static class NutritionCalculator {
    public static NutritionTotals CalculateTotal(
        NutritionFacts facts,
        FoodQuantity quantity) {
        var multiplier = GetMultiplier(facts.Basis, quantity);

        if(multiplier is null) {
            return NutritionTotals.Empty;
        }

        return new NutritionTotals(
            CaloriesKcal: Multiply(facts.CaloriesKcal, multiplier.Value),
            ProteinG: Multiply(facts.ProteinG, multiplier.Value),
            FatG: Multiply(facts.FatG, multiplier.Value),
            CarbsG: Multiply(facts.CarbsG, multiplier.Value));
    }

    private static decimal? GetMultiplier(
        NutritionBasis basis,
        FoodQuantity quantity) {
        return basis switch
        {
            NutritionBasis.Per100Grams when quantity.Unit == FoodUnit.Gram
                => quantity.Value / 100m,

            NutritionBasis.Per100Milliliters when quantity.Unit == FoodUnit.Milliliter
                => quantity.Value / 100m,

            NutritionBasis.PerItem when quantity.Unit == FoodUnit.Piece
                => quantity.Value,

            NutritionBasis.Total
                => 1m,

            _ => null
        };
    }

    private static decimal? Multiply(decimal? value, decimal multiplier) {
        return value is null
            ? null
            : value.Value * multiplier;
    }
}