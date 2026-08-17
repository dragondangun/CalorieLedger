using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Today;

public sealed record TodayDashboardSnapshot(
    DailyNutritionTarget Target,
    NutritionTotals ConsumedTotals,
    IReadOnlyList<FoodDiaryMealSnapshot> Meals,
    WeeklyNutritionSummarySnapshot WeeklySummary,
    IReadOnlyList<TodayActivitySnapshotItem> Activities,
    NutritionGoalDecision GoalDecision,
    bool IsFoodLogComplete
);
