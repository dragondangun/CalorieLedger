namespace CalorieLedger.ViewModels.Today;

public sealed record TodayFoodLogItemViewModel(
    string Name,
    string QuantitySummary,
    string CaloriesSummary,
    string MacrosSummary,
    bool IsApproximate = false,
    decimal? CaloriesKcal = null,
    decimal? ProteinG = null,
    decimal? FatG = null,
    decimal? CarbsG = null) {
    public string AccuracySummary =>
        IsApproximate ? "примерная оценка" : "точная запись";
}