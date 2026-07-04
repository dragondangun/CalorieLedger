using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalorieLedger.ViewModels.Today;

public sealed partial class TodayDashboardViewModel:ObservableObject {
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingCaloriesKcal))]
    [NotifyPropertyChangedFor(nameof(CaloriesSummary))]
    [NotifyPropertyChangedFor(nameof(RemainingCaloriesSummary))]
    private decimal targetCaloriesKcal = 2200m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingCaloriesKcal))]
    [NotifyPropertyChangedFor(nameof(CaloriesSummary))]
    [NotifyPropertyChangedFor(nameof(RemainingCaloriesSummary))]
    private decimal consumedCaloriesKcal = 1350m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal proteinG = 82m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal fatG = 48m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
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

    [RelayCommand]
    private void AddSampleFood() {
        var facts = new NutritionFacts(
            Basis: NutritionBasis.Per100Grams,
            CaloriesKcal: 120m,
            ProteinG: 17m,
            FatG: 5m,
            CarbsG: 3m);

        var quantity = FoodQuantity.Grams(250m);

        var total = NutritionCalculator.CalculateTotal(facts, quantity);

        ConsumedCaloriesKcal += total.CaloriesKcal ?? 0m;
        ProteinG += total.ProteinG ?? 0m;
        FatG += total.FatG ?? 0m;
        CarbsG += total.CarbsG ?? 0m;
    }
}