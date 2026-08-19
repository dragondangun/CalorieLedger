using CalorieLedger.Application.MealPlanning;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelMealPlanFoodTests {
    [Fact]
    public void MealPlanItem_LogFoodOpensPrefilledEditorAndReturnsToPlanOnCancel() {
        var currentDate = new DateOnly(2026, 8, 19);
        var foodDiaryStore = new InMemoryFoodDiaryStore();
        var mealPlanStore = new InMemoryMealPlanStore();
        var mealPlanService = new MealPlanService(mealPlanStore);
        mealPlanService.Save(
            new MealPlan([
                CreateDay(currentDate),
            ])
        );

        var viewModel = CreateViewModel(
            currentDate,
            foodDiaryStore,
            mealPlanStore
        );

        viewModel.OpenMealPlanCommand.Execute(null);

        var plan = Assert.IsType<
            CalorieLedger.ViewModels.MealPlanning.MealPlanManagerViewModel
        >(viewModel.MealPlanManager);
        var item = Assert.Single(Assert.Single(plan.Meals).Items);

        item.LogFoodCommand.Execute(null);

        var editor = Assert.IsType<FoodLogEditorViewModel>(
            viewModel.FoodLogEditor
        );

        Assert.True(viewModel.IsMealPlanOpen);
        Assert.False(viewModel.IsMealPlanVisible);
        Assert.True(viewModel.IsFoodLogEditorVisible);
        Assert.Equal("Овсянка", editor.Name);
        Assert.Equal(MealGroupRole.Breakfast, editor.MealRole);
        Assert.Equal(80m, editor.QuantityValue);
        Assert.Equal(FoodUnit.Gram, editor.QuantityUnit);
        Assert.Equal(NutritionBasis.Per100Grams, editor.NutritionBasis);
        Assert.Equal(350m, editor.CaloriesKcal);

        editor.CancelCommand.Execute(null);

        Assert.Null(viewModel.FoodLogEditor);
        Assert.True(viewModel.IsMealPlanVisible);
    }

    [Fact]
    public void MealPlanItem_SaveCreatesActualFoodDiaryEntry() {
        var currentDate = new DateOnly(2026, 8, 19);
        var foodDiaryStore = new InMemoryFoodDiaryStore();
        var mealPlanStore = new InMemoryMealPlanStore();
        new MealPlanService(mealPlanStore).Save(
            new MealPlan([
                CreateDay(currentDate),
            ])
        );

        var viewModel = CreateViewModel(
            currentDate,
            foodDiaryStore,
            mealPlanStore
        );

        viewModel.OpenMealPlanCommand.Execute(null);
        var item = Assert.Single(
            Assert.Single(viewModel.MealPlanManager!.Meals).Items
        );

        item.LogFoodCommand.Execute(null);
        viewModel.FoodLogEditor!.SaveCommand.Execute(null);

        var meal = Assert.Single(
            foodDiaryStore.GetMeals(
                currentDate,
                currentDate
            )
        );
        var food = Assert.Single(
            foodDiaryStore.GetFoodEntries([meal.Id])
        );

        Assert.Equal("Овсянка", food.Name);
        Assert.Equal(FoodQuantity.Grams(80m), food.Quantity);
        Assert.Equal(FoodLogSource.Manual, food.Source);
        Assert.True(viewModel.IsMealPlanVisible);
    }

    private static MainViewModel CreateViewModel(
        DateOnly currentDate,
        IFoodDiaryStore foodDiaryStore,
        IMealPlanStore mealPlanStore
    ) {
        var profileStore = new InMemoryUserNutritionProfileStore(
            new SampleUserNutritionProfileProvider().GetCurrentProfile()
        );

        return new MainViewModel(
            new InMemoryBodyMeasurementStore(),
            profileStore,
            foodDiaryStore,
            mealPlanStore,
            new FixedCurrentDateProvider(currentDate)
        );
    }

    private static MealPlanDay CreateDay(DateOnly date) {
        return new MealPlanDay(
            Date: date,
            Meals: [
                new MealPlanMeal(
                    Name: "Завтрак",
                    Role: MealGroupRole.Breakfast,
                    Time: new TimeOnly(9, 0),
                    Items: [
                        new MealPlanItem(
                            Name: "Овсянка",
                            Quantity: FoodQuantity.Grams(80m),
                            FridgeItemId: null,
                            Nutrition: new NutritionTotals(
                                CaloriesKcal: 280m,
                                ProteinG: 10m,
                                FatG: 6m,
                                CarbsG: 48m
                            )
                        ),
                    ]
                ),
            ]
        );
    }
}
