using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;
using System;

namespace CalorieLedger.Application.Meals;

public sealed record FoodDiaryFoodSnapshotItem(
    Guid Id,
    string Name,
    FoodQuantity Quantity,
    NutritionTotals Totals,
    bool IsApproximate = false,
    string? Note = null
);
