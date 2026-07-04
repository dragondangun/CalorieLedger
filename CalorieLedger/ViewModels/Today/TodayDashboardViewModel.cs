using CommunityToolkit.Mvvm.ComponentModel;

namespace CalorieLedger.ViewModels.Today;

public sealed partial class TodayDashboardViewModel:ObservableObject {
    [ObservableProperty]
    private decimal targetCaloriesKcal = 2200m;

    [ObservableProperty]
    private decimal consumedCaloriesKcal = 1350m;

    [ObservableProperty]
    private decimal proteinG = 82m;

    [ObservableProperty]
    private decimal fatG = 48m;

    [ObservableProperty]
    private decimal carbsG = 145m;

    public decimal RemainingCaloriesKcal => TargetCaloriesKcal - ConsumedCaloriesKcal;

    public string CaloriesSummary =>
        $"{ConsumedCaloriesKcal:0} / {TargetCaloriesKcal:0} ккал";

    public string RemainingCaloriesSummary =>
        RemainingCaloriesKcal >= 0
            ? $"Осталось {RemainingCaloriesKcal:0} ккал"
            : $"Превышение {-RemainingCaloriesKcal:0} ккал";

    public string MacrosSummary =>
        $"Б: {ProteinG:0.#} г · Ж: {FatG:0.#} г · У: {CarbsG:0.#} г";
}