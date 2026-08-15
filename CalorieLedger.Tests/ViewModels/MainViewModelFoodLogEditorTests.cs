using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Meals;
using System;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelFoodLogEditorTests {
    [Fact]
    public void AddFood_OpensEditorAndHidesTodayDashboard() {
        var currentDate = new DateOnly(2026, 8, 15);

        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore()
        );

        viewModel.Today.AddFoodCommand.Execute(null);

        Assert.True(viewModel.IsFoodLogEditorOpen);

        Assert.False(viewModel.IsTodayDashboardVisible);

        Assert.NotNull(viewModel.FoodLogEditor);
    }

    [Fact]
    public void SaveFood_PersistsEntryClosesEditorAndRefreshesToday() {
        var currentDate = new DateOnly(2026, 8, 15);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var viewModel = CreateViewModel(
            currentDate,
            foodDiaryStore
        );

        viewModel.Today.AddFoodCommand.Execute(null);

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

        Assert.False(
            viewModel.IsFoodLogEditorOpen
        );

        Assert.True(
            viewModel.IsTodayDashboardVisible
        );

        Assert.Equal(
            300m,
            viewModel.Today.ConsumedCaloriesKcal
        );

        var meal = Assert.Single(
            foodDiaryStore.GetMeals(
                currentDate,
                currentDate
            )
        );

        Assert.Single(
            foodDiaryStore.GetFoodEntries(
                [meal.Id]
            )
        );
    }

    [Fact]
    public void CancelFoodEditing_DoesNotPersistEntry() {
        var currentDate = new DateOnly(2026, 8, 15);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var viewModel = CreateViewModel(
            currentDate,
            foodDiaryStore
        );

        viewModel.Today.AddFoodCommand.Execute(null);

        var editor = Assert.IsType<FoodLogEditorViewModel>(
            viewModel.FoodLogEditor
        );

        editor.CancelCommand.Execute(null);

        Assert.False(viewModel.IsFoodLogEditorOpen);

        Assert.Empty(
            foodDiaryStore.GetMeals(
                currentDate,
                currentDate
            )
        );
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
            new FixedCurrentDateProvider(
                currentDate
            )
        );
    }
}
