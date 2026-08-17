using CalorieLedger.Domain.Meals;
using System;
using System.Collections.Generic;

namespace CalorieLedger.Application.Meals;

public sealed record FoodDiaryMealSnapshot(
    string Name,
    MealGroupRole Role,
    TimeOnly? EatenAt,
    IReadOnlyList<FoodDiaryFoodSnapshotItem> FoodItems
);
