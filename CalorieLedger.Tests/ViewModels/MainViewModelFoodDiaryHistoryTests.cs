using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelFoodDiaryHistoryTests {
    [Fact]
    public void OpenHistory_PreviousDay_LoadsPastFood() {
        var currentDate = new DateOnly(2026, 8, 17);

        var previousDate = currentDate.AddDays(-1);

        var store = new InMemoryFoodDiaryStore();

        var meal = CreateMeal(previousDate);

        store.SaveMeal(meal);

        store.SaveFoodEntry(
            CreateFood(
                meal.Id,
                "Вчерашний ужин",
                700m
            )
        );

        var viewModel = CreateViewModel(
            currentDate,
            store
        );

        viewModel.OpenFoodDiaryHistoryCommand.Execute(null);

        var history = Assert.IsType<FoodDiaryHistoryViewModel>(
            viewModel.FoodDiaryHistory
        );

        Assert.Equal(
            currentDate,
            history.SelectedDate
        );

        history.PreviousDayCommand.Execute(null);

        Assert.Equal(
            previousDate,
            history.SelectedDate
        );

        Assert.Equal(
            700m,
            history.ConsumedCaloriesKcal
        );

        var food = Assert.Single(
            Assert.Single(history.MealGroups).FoodItems
        );

        Assert.Equal(
            "Вчерашний ужин",
            food.Name
        );
    }

    [Fact]
    public void AddFood_FromPastDay_PersistsToSelectedDateAndRefreshesHistory() {
        var currentDate = new DateOnly(2026, 8, 17);

        var previousDate = currentDate.AddDays(-1);

        var store = new InMemoryFoodDiaryStore();

        var viewModel = CreateViewModel(
            currentDate,
            store
        );

        viewModel.OpenFoodDiaryHistoryCommand.Execute(null);

        var history = Assert.IsType<FoodDiaryHistoryViewModel>(viewModel.FoodDiaryHistory);

        history.PreviousDayCommand.Execute(null);

        history.AddFoodCommand.Execute(null);

        var editor = Assert.IsType<FoodLogEditorViewModel>(viewModel.FoodLogEditor);

        editor.Name = "Творог";

        editor.QuantityValue = 250m;

        editor.QuantityUnit = FoodUnit.Gram;
        editor.NutritionBasis = NutritionBasis.Per100Grams;

        editor.CaloriesKcal = 120m;
        editor.ProteinG = 17m;
        editor.FatG = 5m;
        editor.CarbsG = 3m;

        editor.SaveCommand.Execute(null);

        Assert.Null(viewModel.FoodLogEditor);

        Assert.NotNull(viewModel.FoodDiaryHistory);

        Assert.Equal(
            previousDate,
            history.SelectedDate
        );

        Assert.Equal(
            300m,
            history.ConsumedCaloriesKcal
        );

        var selectedWeekDay = Assert.Single(history.WeekDays, day => day.IsSelected);

        Assert.Equal(
            previousDate,
            selectedWeekDay.Date
        );

        Assert.Equal(
            "300 ккал",
            selectedWeekDay.CaloriesSummary
        );

        Assert.False(selectedWeekDay.IsComplete);

        Assert.Equal(
            0m,
            viewModel.Today.ConsumedCaloriesKcal
        );

        var meal = Assert.Single(store.GetMeals(previousDate, previousDate));

        Assert.Single(store.GetFoodEntries([meal.Id]));
    }

    [Fact]
    public void ToggleCompletion_ForPastDay_ChangesOnlySelectedDate() {
        var currentDate = new DateOnly(2026, 8, 17);

        var previousDate = currentDate.AddDays(-1);

        var store = new InMemoryFoodDiaryStore();

        var viewModel = CreateViewModel(
            currentDate,
            store
        );

        viewModel.OpenFoodDiaryHistoryCommand.Execute(null);

        var history = Assert.IsType<FoodDiaryHistoryViewModel>(
            viewModel.FoodDiaryHistory
        );

        history.PreviousDayCommand.Execute(null);

        history.ToggleCompletionCommand.Execute(null);

        Assert.True(history.IsComplete);

        Assert.Contains(
            previousDate,
            store.GetCompletedDates(
                previousDate,
                currentDate
            )
        );

        Assert.False(viewModel.Today.IsFoodLogComplete);
    }

    private static MainViewModel CreateViewModel(
        DateOnly currentDate,
        IFoodDiaryStore foodDiaryStore
    ) {
        var profileStore = new InMemoryUserNutritionProfileStore(
            new SampleUserNutritionProfileProvider().GetCurrentProfile()
        );

        return new MainViewModel(
            new InMemoryBodyMeasurementStore(),
            profileStore,
            foodDiaryStore,
            new FixedCurrentDateProvider(currentDate)
        );
    }

    private static MealEntry CreateMeal(DateOnly date) {
        return new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Ужин",
            Role: MealGroupRole.Dinner
        );
    }

    private static FoodLogEntry CreateFood(
        Guid mealId,
        string name,
        decimal caloriesKcal
    ) {
        return new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: mealId,
            Name: name,
            Quantity: FoodQuantity.Portions(1m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Total,
                CaloriesKcal: caloriesKcal,
                ProteinG: 30m,
                FatG: 20m,
                CarbsG: 50m
            ),
            Source: FoodLogSource.Manual
        );
    }
}
