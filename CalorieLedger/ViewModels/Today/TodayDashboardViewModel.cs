using CalorieLedger.Application.Today;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Today;

public sealed partial class TodayDashboardViewModel:ObservableObject {
    public decimal TargetCaloriesKcal => target.CaloriesKcal;

    public decimal? TargetProteinG => target.ProteinG;

    public decimal? TargetFatG => target.FatG;

    public decimal? TargetCarbsG => target.CarbsG;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingCaloriesKcal))]
    [NotifyPropertyChangedFor(nameof(CaloriesSummary))]
    [NotifyPropertyChangedFor(nameof(RemainingCaloriesSummary))]
    private decimal consumedCaloriesKcal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal proteinG;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal fatG;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal carbsG;

    public ObservableCollection<TodayFoodLogItemViewModel> FoodItems { get; } = [];

    public decimal RemainingCaloriesKcal => TargetCaloriesKcal - ConsumedCaloriesKcal;

    public string CaloriesSummary =>
        $"{ConsumedCaloriesKcal:0} / {TargetCaloriesKcal:0} ккал";

    public string RemainingCaloriesSummary =>
        RemainingCaloriesKcal >= 0
            ? $"Осталось {RemainingCaloriesKcal:0} ккал"
            : $"Превышение {-RemainingCaloriesKcal:0} ккал";

    public string MacrosSummary =>
        $"Б: {ProteinG:0.#}/{FormatTarget(TargetProteinG)} г · " +
        $"Ж: {FatG:0.#}/{FormatTarget(TargetFatG)} г · " +
        $"У: {CarbsG:0.#}/{FormatTarget(TargetCarbsG)} г";

    private readonly DailyNutritionTarget target;

    public TodayDashboardViewModel(TodayDashboardSnapshot snapshot) {
        target = snapshot.Target;

        ConsumedCaloriesKcal = snapshot.ConsumedTotals.CaloriesKcal ?? 0m;
        ProteinG = snapshot.ConsumedTotals.ProteinG ?? 0m;
        FatG = snapshot.ConsumedTotals.FatG ?? 0m;
        CarbsG = snapshot.ConsumedTotals.CarbsG ?? 0m;
    }

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

        FoodItems.Add(new TodayFoodLogItemViewModel(
            Name: "Творог тестовый",
            QuantitySummary: "250 г",
            CaloriesSummary: $"{total.CaloriesKcal ?? 0m:0} ккал",
            MacrosSummary: $"Б: {total.ProteinG ?? 0m:0.#} г · Ж: {total.FatG ?? 0m:0.#} г · У: {total.CarbsG ?? 0m:0.#} г"));
    }

    [RelayCommand]
    private void MarkOvereating() {
        const decimal estimatedCalories = 1500m;

        ConsumedCaloriesKcal += estimatedCalories;

        FoodItems.Add(new TodayFoodLogItemViewModel(
            Name: "Праздник / переедание",
            QuantitySummary: "количество неизвестно",
            CaloriesSummary: $"+{estimatedCalories:0} ккал",
            MacrosSummary: "Б/Ж/У неизвестны",
            IsApproximate: true));
    }

    private static string FormatTarget(decimal? value) {
        return value is null ? "—" : $"{value.Value:0.#}";
    }
}