using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Today;

public sealed record TodayDashboardSnapshot(
    DailyNutritionTarget Target,
    NutritionTotals ConsumedTotals,
    IReadOnlyList<TodayMealSnapshot> Meals,
    WeeklyNutritionSummarySnapshot WeeklySummary,
    IReadOnlyList<TodayActivitySnapshotItem> Activities);