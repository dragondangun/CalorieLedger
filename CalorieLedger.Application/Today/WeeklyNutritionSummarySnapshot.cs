using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Today;

public sealed record WeeklyNutritionSummarySnapshot(
    IReadOnlyList<DailyNutritionSummarySnapshot> Days) {
    public decimal AverageCaloriesKcal =>
        Days.Count == 0
            ? 0m
            : Days.Average(x => x.ConsumedTotals.CaloriesKcal ?? 0m);

    public decimal AverageProteinG =>
        Days.Count == 0
            ? 0m
            : Days.Average(x => x.ConsumedTotals.ProteinG ?? 0m);

    public decimal AverageFatG =>
        Days.Count == 0
            ? 0m
            : Days.Average(x => x.ConsumedTotals.FatG ?? 0m);

    public decimal AverageCarbsG =>
        Days.Count == 0
            ? 0m
            : Days.Average(x => x.ConsumedTotals.CarbsG ?? 0m);
}