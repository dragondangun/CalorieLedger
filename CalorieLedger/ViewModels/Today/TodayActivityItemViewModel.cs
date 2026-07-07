namespace CalorieLedger.ViewModels.Today;

public sealed record TodayActivityItemViewModel(
    string Name,
    decimal BurnedCaloriesKcal,
    string TimeSummary,
    string DurationSummary) {
    public string CaloriesSummary => $"{BurnedCaloriesKcal:0} ккал";
}