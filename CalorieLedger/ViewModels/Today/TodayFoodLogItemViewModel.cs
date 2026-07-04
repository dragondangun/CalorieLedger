namespace CalorieLedger.ViewModels.Today;

public sealed record TodayFoodLogItemViewModel(
    string Name,
    string QuantitySummary,
    string CaloriesSummary,
    string MacrosSummary,
    bool IsApproximate = false) {
    public string AccuracySummary =>
        IsApproximate ? "примерная оценка" : "точная запись";
}