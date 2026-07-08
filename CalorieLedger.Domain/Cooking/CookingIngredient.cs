using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Cooking;

public sealed record CookingIngredient(
    Guid Id,
    string Name,
    FoodQuantity Quantity,
    NutritionFacts Nutrition,
    CookingIngredientSource Source = CookingIngredientSource.Manual,
    Guid? SourceId = null,
    string? Note = null);