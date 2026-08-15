using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelFoodDiaryTests {
    [Fact]
    public void ToggleFoodLogCompletion_CompletesCurrentDay() {
        var currentDate = new DateOnly(2026, 8, 10);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var viewModel = CreateViewModel(
            currentDate,
            foodDiaryStore
        );

        viewModel.Today.ToggleFoodLogCompletionCommand.Execute(null);

        Assert.True(viewModel.Today.IsFoodLogComplete);

        Assert.Contains(
            currentDate,
            foodDiaryStore.GetCompletedDates(
                currentDate,
                currentDate
            )
        );
    }

    [Fact]
    public void MarkOvereating_PersistsApproximateFood() {
        var currentDate = new DateOnly(2026, 8, 10);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var viewModel = CreateViewModel(
            currentDate,
            foodDiaryStore
        );

        viewModel.Today.MarkOvereatingCommand.Execute(null);

        var meal = Assert.Single(
            foodDiaryStore.GetMeals(
                currentDate,
                currentDate
            )
        );

        var foodEntry = Assert.Single(
            foodDiaryStore.GetFoodEntries(
                [meal.Id]
            )
        );

        Assert.True(foodEntry.IsApproximate);

        Assert.Equal(
            1500m,
            NutritionCalculator.CalculateTotal(
                foodEntry.Nutrition,
                foodEntry.Quantity
            ).CaloriesKcal
        );

        Assert.Equal(
            1500m,
            viewModel.Today.ConsumedCaloriesKcal
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
