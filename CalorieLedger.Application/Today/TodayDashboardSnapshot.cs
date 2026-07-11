using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Today;

public sealed record TodayDashboardSnapshot(
    DailyNutritionTarget Target,
    NutritionTotals ConsumedTotals,
    IReadOnlyList<TodayMealSnapshot> Meals,
    WeeklyNutritionSummarySnapshot WeeklySummary,
    IReadOnlyList<TodayActivitySnapshotItem> Activities,
    NutritionGoalDecision GoalDecision);