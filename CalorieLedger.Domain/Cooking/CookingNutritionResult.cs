using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Cooking;

public sealed record CookingNutritionResult(
    NutritionTotals TotalNutrition,
    NutritionFacts NutritionPer100Grams);