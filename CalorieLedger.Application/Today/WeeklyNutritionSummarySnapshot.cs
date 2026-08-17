namespace CalorieLedger.Application.Today;

public sealed record WeeklyNutritionSummarySnapshot(IReadOnlyList<DailyNutritionSummarySnapshot> Days) {
    public int EnergyCompleteDayCount => Days.Count(day => day.IsEnergyComplete);

    public int MacroCompleteDayCount => Days.Count(day => day.AreMacrosComplete);

    public decimal? AverageCaloriesKcal => CalculateAverage(
        day => day.IsEnergyComplete,
        day => day.ConsumedTotals.CaloriesKcal
    );

    public decimal? AverageProteinG => CalculateAverage(
        day => day.AreMacrosComplete,
        day => day.ConsumedTotals.ProteinG
    );

    public decimal? AverageFatG => CalculateAverage(
        day => day.AreMacrosComplete,
        day => day.ConsumedTotals.FatG
    );

    public decimal? AverageCarbsG => CalculateAverage(
        day => day.AreMacrosComplete,
        day => day.ConsumedTotals.CarbsG
    );

    private decimal? CalculateAverage(
        Func<DailyNutritionSummarySnapshot, bool> predicate,
        Func<DailyNutritionSummarySnapshot, decimal?> selector
    ) {
        var values = Days
            .Where(predicate)
            .Select(day => selector(day) ?? 0m)
            .ToList();

        return values.Count == 0 ? null : values.Average();
    }
}
