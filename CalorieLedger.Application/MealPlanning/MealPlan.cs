using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.MealPlanning;

public sealed record MealPlan(
    IReadOnlyList<MealPlanDay> Days
);

public sealed record MealPlanDay(
    DateOnly Date,
    IReadOnlyList<MealPlanMeal> Meals
);

public sealed record MealPlanMeal(
    string Name,
    MealGroupRole Role,
    TimeOnly? Time,
    IReadOnlyList<MealPlanItem> Items,
    string? Note = null
);

public sealed record MealPlanItem(
    string Name,
    FoodQuantity Quantity,
    Guid? FridgeItemId,
    NutritionTotals Nutrition,
    string? Note = null
);
