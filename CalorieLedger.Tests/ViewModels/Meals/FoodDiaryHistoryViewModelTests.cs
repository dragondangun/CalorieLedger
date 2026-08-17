using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels.Meals;

public sealed class FoodDiaryHistoryViewModelTests {
    [Fact]
    public void Constructor_CurrentWeek_BuildsSevenDaysAndDisablesFutureDays() {
        var currentDate = new DateOnly(2026, 8, 19);

        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore()
        );

        Assert.Equal(
            7,
            viewModel.WeekDays.Count
        );

        Assert.Equal(
            new DateOnly(2026, 8, 17),
            viewModel.WeekDays[0].Date
        );

        Assert.Equal(
            new DateOnly(2026, 8, 23),
            viewModel.WeekDays[^1].Date
        );

        Assert.Equal(
            3,
            viewModel.WeekDays.Count(day => day.IsAvailable)
        );

        Assert.Equal(
            4,
            viewModel.WeekDays.Count(day => !day.IsAvailable)
        );

        var today = Assert.Single(viewModel.WeekDays, day => day.IsToday);

        Assert.True(today.IsSelected);

        Assert.False(viewModel.NextWeekCommand.CanExecute(null));
    }

    [Fact]
    public void SelectWeekDay_LoadsSelectedDayFood() {
        var currentDate = new DateOnly(2026, 8, 19);

        var selectedDate = new DateOnly(2026, 8, 18);

        var store = new InMemoryFoodDiaryStore();

        var meal = CreateMeal(selectedDate);

        store.SaveMeal(meal);

        store.SaveFoodEntry(
            CreateFood(
                meal.Id,
                caloriesKcal: 600m
            )
        );

        store.SetDateComplete(selectedDate, true);

        var viewModel = CreateViewModel(currentDate, store);

        var day = Assert.Single(viewModel.WeekDays, item => item.Date == selectedDate);

        day.SelectCommand.Execute(null);

        Assert.Equal(
            selectedDate,
            viewModel.SelectedDate
        );

        Assert.Equal(
            600m,
            viewModel.ConsumedCaloriesKcal
        );

        var selectedDay = Assert.Single(viewModel.WeekDays, item => item.IsSelected);

        Assert.Equal(
            selectedDate,
            selectedDay.Date
        );

        Assert.True(selectedDay.IsEnergyComplete);

        Assert.True(selectedDay.AreMacrosComplete);

        Assert.Equal(
            "данные полны",
            selectedDay.StatusSummary
        );
    }

    [Fact]
    public void PreviousAndNextWeek_PreserveWeekdayWithoutEnteringFuture() {
        var currentDate = new DateOnly(2026, 8, 19);

        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore()
        );

        viewModel.PreviousWeekCommand.Execute(null);

        Assert.Equal(
            new DateOnly(2026, 8, 12),
            viewModel.SelectedDate
        );

        Assert.All(
            viewModel.WeekDays,
            day => Assert.True(day.IsAvailable)
        );

        Assert.True(viewModel.NextWeekCommand.CanExecute(null));

        viewModel.NextWeekCommand.Execute(null);

        Assert.Equal(
            currentDate,
            viewModel.SelectedDate
        );

        Assert.False(viewModel.NextWeekCommand.CanExecute(null));
    }

    [Fact]
    public void Refresh_AfterDiaryChange_UpdatesSelectedWeekDay() {
        var currentDate = new DateOnly(2026, 8, 19);

        var selectedDate = new DateOnly(2026, 8, 18);

        var store = new InMemoryFoodDiaryStore();

        var viewModel = CreateViewModel(
            currentDate,
            store
        );

        viewModel.WeekDays
            .Single(day => day.Date == selectedDate)
            .SelectCommand
            .Execute(null);

        var meal = CreateMeal(selectedDate);

        store.SaveMeal(meal);

        store.SaveFoodEntry(
            CreateFood(
                meal.Id,
                caloriesKcal: 750m
            )
        );

        store.SetDateComplete(
            selectedDate,
            true
        );

        viewModel.Refresh();

        var day = Assert.Single(viewModel.WeekDays, item => item.Date == selectedDate);

        Assert.True(day.IsSelected);

        Assert.True(day.IsEnergyComplete);

        Assert.Equal(
            "750 ккал",
            day.CaloriesSummary
        );

        Assert.Equal(
            750m,
            viewModel.ConsumedCaloriesKcal
        );
    }

    private static FoodDiaryHistoryViewModel CreateViewModel(DateOnly currentDate, IFoodDiaryStore foodDiaryStore) {
        return new FoodDiaryHistoryViewModel(
            snapshotProvider: new FoodDiaryDaySnapshotProvider(foodDiaryStore),
            currentDate: currentDate,
            addFood: _ => { },
            addApproximateFood: _ => { },
            editFood: _ => { },
            deleteFood: _ => { },
            setFoodLogComplete: (_, _) => { },
            onClosed: () => { }
        );
    }

    private static MealEntry CreateMeal(DateOnly date) {
        return new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Обед",
            Role: MealGroupRole.Lunch
        );
    }

    private static FoodLogEntry CreateFood(Guid mealId, decimal caloriesKcal) {
        return new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: mealId,
            Name: "Тестовая еда",
            Quantity: FoodQuantity.Portions(1m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Total,
                CaloriesKcal: caloriesKcal,
                ProteinG: 30m,
                FatG: 20m,
                CarbsG: 60m
            ),
            Source: FoodLogSource.Manual
        );
    }
}
