using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Cooking;

public sealed record CookingNutritionLlmParseResult(
    bool IsSuccess,
    NutritionFacts? NutritionPer100Grams,
    string? Note,
    IReadOnlyList<CookingNutritionLlmParseError> Errors
);
