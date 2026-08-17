using CalorieLedger.Domain.Nutrition;
using System;
using System.Collections.Generic;

namespace CalorieLedger.Application.Meals;

public sealed record FoodDiaryDaySnapshot(
    DateOnly Date,
    IReadOnlyList<FoodDiaryMealSnapshot> Meals,
    NutritionTotals ConsumedTotals,
    bool IsComplete
);
