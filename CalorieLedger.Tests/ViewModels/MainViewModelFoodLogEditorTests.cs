using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
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

        Assert.False(viewModel.IsFoodLogEditorOpen);

        Assert.True(viewModel.IsTodayDashboardVisible);

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

        Assert.Single(foodDiaryStore.GetFoodEntries([meal.Id]));
    }

    [Fact]
    public void CancelFoodEditing_DoesNotPersistEntry() {
        var currentDate = new DateOnly(2026, 8, 15);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var viewModel = CreateViewModel(currentDate, foodDiaryStore);

        viewModel.Today.AddFoodCommand.Execute(null);

        var editor = Assert.IsType<FoodLogEditorViewModel>(viewModel.FoodLogEditor);

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
            new FixedCurrentDateProvider(currentDate)
        );
    }

    [Fact]
    public void EditFood_OpensExistingEntryAndSavesChanges() {
        var currentDate = new DateOnly(2026, 8, 15);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var meal = new MealEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            Name: "Перекусы",
            Role: MealGroupRole.Snack
        );

        var food = new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: meal.Id,
            Name: "Творог",
            Quantity: FoodQuantity.Grams(200m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 120m,
                ProteinG: 17m,
                FatG: 5m,
                CarbsG: 3m
            ),
            Source: FoodLogSource.Manual
        );

        foodDiaryStore.SaveMeal(meal);

        foodDiaryStore.SaveFoodEntry(food);

        var viewModel = CreateViewModel(
            currentDate,
            foodDiaryStore
        );

        var foodItem = Assert.Single(Assert.Single(viewModel.Today.MealGroups).FoodItems);

        foodItem.EditCommand.Execute(null);

        var editor = Assert.IsType<FoodLogEditorViewModel>(
            viewModel.FoodLogEditor
        );

        Assert.Equal(
            "Творог",
            editor.Name
        );

        editor.QuantityValue = 250m;

        editor.SaveCommand.Execute(null);

        Assert.Null(viewModel.FoodLogEditor);

        Assert.Equal(
            300m,
            viewModel.Today.ConsumedCaloriesKcal
        );
    }

    [Fact]
    public void DeleteFood_Confirmed_RemovesEntryAndMealFromToday() {
        var currentDate = new DateOnly(2026, 8, 15);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var meal = new MealEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            Name: "Перекусы",
            Role: MealGroupRole.Snack
        );

        var food = new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: meal.Id,
            Name: "Яблоко",
            Quantity: FoodQuantity.Grams(150m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 52m,
                ProteinG: 0.3m,
                FatG: 0.2m,
                CarbsG: 14m
            ),
            Source: FoodLogSource.Manual
        );

        foodDiaryStore.SaveMeal(meal);

        foodDiaryStore.SaveFoodEntry(food);

        var viewModel = CreateViewModel(
            currentDate,
            foodDiaryStore
        );

        var foodItem = Assert.Single(
            Assert.Single(viewModel.Today.MealGroups).FoodItems
        );

        foodItem.DeleteCommand.Execute(null);

        Assert.True(foodItem.IsDeleteConfirmationVisible);

        foodItem.ConfirmDeleteCommand.Execute(null);

        Assert.Empty(viewModel.Today.MealGroups);

        Assert.Equal(
            0m,
            viewModel.Today.ConsumedCaloriesKcal
        );

        Assert.Null(foodDiaryStore.GetFoodEntry(food.Id));

        Assert.Null(foodDiaryStore.GetMeal(meal.Id));
    }
}
