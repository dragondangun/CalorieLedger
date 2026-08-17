using CalorieLedger.Domain.Nutrition;
using System;
using System.Collections.Generic;

namespace CalorieLedger.Application.Meals;

public sealed record FoodDiaryDaySnapshot(
    DateOnly Date,
    IReadOnlyList<FoodDiaryMealSnapshot> Meals,
    NutritionTotals ConsumedTotals,
    bool IsComplete,
    bool HasUnknownCalories,
    bool HasUnknownProtein,
    bool HasUnknownFat,
    bool HasUnknownCarbs
) {
    public bool IsEnergyComplete => IsComplete && !HasUnknownCalories;

    public bool AreMacrosComplete =>
        IsComplete
        && !HasUnknownProtein
        && !HasUnknownFat
        && !HasUnknownCarbs;
}
