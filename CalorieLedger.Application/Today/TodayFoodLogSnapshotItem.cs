using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Today;

public sealed record TodayFoodLogSnapshotItem(
    string Name,
    FoodQuantity Quantity,
    NutritionTotals Totals,
    bool IsApproximate = false,
    string? Note = null);