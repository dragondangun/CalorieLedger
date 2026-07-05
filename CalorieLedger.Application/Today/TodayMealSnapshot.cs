using CalorieLedger.Domain.Meals;

namespace CalorieLedger.Application.Today;

public sealed record TodayMealSnapshot(
    string Name,
    MealGroupRole Role,
    TimeOnly? EatenAt,
    IReadOnlyList<TodayFoodLogSnapshotItem> FoodItems);