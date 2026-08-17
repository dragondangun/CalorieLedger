using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Meals;

public sealed partial class FoodDiaryMealGroupViewModel:ObservableObject {
    private decimal caloriesKcal;
    private decimal proteinG;
    private decimal fatG;
    private decimal carbsG;

    public string Name { get; }

    public string TimeSummary { get; }

    public ObservableCollection<FoodDiaryFoodItemViewModel> FoodItems { get; } = [];

    public bool HasFoodItems => FoodItems.Count > 0;

    public string CaloriesSummary => $"{caloriesKcal:0} ккал";

    public string MacrosSummary => $"Б: {proteinG:0.#} г · Ж: {fatG:0.#} г · У: {carbsG:0.#} г";

    public FoodDiaryMealGroupViewModel(
        string name,
        string timeSummary,
        IEnumerable<FoodDiaryFoodItemViewModel> foodItems
    ) {
        Name = name;

        TimeSummary = timeSummary;

        foreach(var item in foodItems) {
            AddFoodItem(item);
        }
    }

    public void AddFoodItem(FoodDiaryFoodItemViewModel item) {
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
