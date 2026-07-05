using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Today;

public sealed partial class TodayMealGroupViewModel:ObservableObject {
    private decimal caloriesKcal;
    private decimal proteinG;
    private decimal fatG;
    private decimal carbsG;

    public TodayMealGroupViewModel(
        string name,
        string timeSummary,
        IEnumerable<TodayFoodLogItemViewModel> foodItems) {
        Name = name;
        TimeSummary = timeSummary;

        foreach(var item in foodItems) {
            AddFoodItem(item);
        }
    }

    public string Name { get; }

    public string TimeSummary { get; }

    public ObservableCollection<TodayFoodLogItemViewModel> FoodItems { get; } = [];

    public bool HasFoodItems => FoodItems.Count > 0;

    public string CaloriesSummary => $"{caloriesKcal:0} ккал";

    public string MacrosSummary =>
        $"Б: {proteinG:0.#} г · Ж: {fatG:0.#} г · У: {carbsG:0.#} г";

    public void AddFoodItem(TodayFoodLogItemViewModel item) {
        FoodItems.Add(item);

        caloriesKcal += item.CaloriesKcal ?? 0m;
        proteinG += item.ProteinG ?? 0m;
        fatG += item.FatG ?? 0m;
        carbsG += item.CarbsG ?? 0m;

        OnPropertyChanged(nameof(HasFoodItems));
        OnPropertyChanged(nameof(CaloriesSummary));
        OnPropertyChanged(nameof(MacrosSummary));
    }
}