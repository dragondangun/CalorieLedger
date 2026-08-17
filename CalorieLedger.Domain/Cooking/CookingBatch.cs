using System;
using System.Collections.Generic;

namespace CalorieLedger.Domain.Cooking;

public sealed record CookingBatch(
    Guid Id,
    Guid SessionId,
    string Name,
    IReadOnlyList<CookingIngredient> Ingredients,
    decimal OutputWeightG,
    CookingNutritionResult Nutrition,
    DateOnly CookedDate,
    Guid OutputFridgeItemId,
    string? Note = null
);
