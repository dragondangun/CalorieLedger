using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Today;

public sealed class TodayMealGroupViewModel {
    public TodayMealGroupViewModel(
        string name,
        string timeSummary,
        IEnumerable<TodayFoodLogItemViewModel> foodItems) {
        Name = name;
        TimeSummary = timeSummary;
        FoodItems = new ObservableCollection<TodayFoodLogItemViewModel>(foodItems);
    }

    public string Name { get; }

    public string TimeSummary { get; }

    public ObservableCollection<TodayFoodLogItemViewModel> FoodItems { get; }

    public bool HasFoodItems => FoodItems.Count > 0;
}