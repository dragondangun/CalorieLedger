using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Cooking;

public static class CookingNutritionCalculator {
    public static CookingNutritionResult Calculate(CookingSessionDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        if(draft.OutputWeightG <= 0m) {
            throw new ArgumentException(
                "Cooked dish output weight must be greater than zero.",
                nameof(draft)
            );
        }

        if(draft.NutritionPer100GramsOverride is not null) {
            return CalculateFromOverride(draft);
        }

        var ingredientTotals = draft.Ingredients
            .Select(ingredient => NutritionCalculator.CalculateTotal(
                ingredient.Nutrition,
                ingredient.Quantity
            ))
            .ToArray();

        var totalNutrition = SumTotals(ingredientTotals);
        var nutritionPer100Grams = CalculatePer100Grams(
            totalNutrition,
            draft.OutputWeightG
        );

        return new CookingNutritionResult(
            TotalNutrition: totalNutrition,
            NutritionPer100Grams: nutritionPer100Grams
        );
    }

    private static CookingNutritionResult CalculateFromOverride(CookingSessionDraft draft) {
        var nutrition = draft.NutritionPer100GramsOverride!;

        if(nutrition.Basis != NutritionBasis.Per100Grams) {
            throw new ArgumentException(
                "Cooked dish nutrition override must use per-100-gram basis.",
                nameof(draft)
            );
        }

        var totalNutrition = NutritionCalculator.CalculateTotal(
            nutrition,
            FoodQuantity.Grams(draft.OutputWeightG)
        );

        return new CookingNutritionResult(
            TotalNutrition: totalNutrition,
            NutritionPer100Grams: nutrition
        );
    }

    private static NutritionTotals SumTotals(
        IReadOnlyCollection<NutritionTotals> totals
    ) {
        return new NutritionTotals(
            CaloriesKcal: SumOrNull(totals.Select(x => x.CaloriesKcal)),
            ProteinG: SumOrNull(totals.Select(x => x.ProteinG)),
            FatG: SumOrNull(totals.Select(x => x.FatG)),
            CarbsG: SumOrNull(totals.Select(x => x.CarbsG))
        );
    }

    private static NutritionFacts CalculatePer100Grams(
        NutritionTotals total,
        decimal outputWeightG
    ) {
        var multiplier = 100m / outputWeightG;

        return new NutritionFacts(
            Basis: NutritionBasis.Per100Grams,
            CaloriesKcal: Multiply(total.CaloriesKcal, multiplier),
            ProteinG: Multiply(total.ProteinG, multiplier),
            FatG: Multiply(total.FatG, multiplier),
            CarbsG: Multiply(total.CarbsG, multiplier)
        );
    }

    private static decimal? SumOrNull(IEnumerable<decimal?> values) {
        decimal sum = 0m;

        foreach(var value in values) {
            if(value is null) {
                return null;
            }

            sum += value.Value;
        }

        return sum;
    }

    private static decimal? Multiply(decimal? value, decimal multiplier) {
        return value is null
            ? null
            : value.Value * multiplier;
    }
}
