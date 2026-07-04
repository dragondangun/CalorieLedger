using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Meals;

public sealed record FoodLogEntry(
    Guid Id,
    Guid MealEntryId,
    string Name,
    FoodQuantity Quantity,
    NutritionFacts Nutrition,
    FoodLogSource Source,
    Guid? SourceId = null,
    bool IsApproximate = false,
    string? Note = null);