using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.Meals;

public sealed class FoodDiaryDaySnapshotProviderTests {
    [Fact]
    public void GetRange_ReturnsEveryDateAndAggregatesFood() {
        var endDate = new DateOnly(2026, 8, 17);

        var startDate = endDate.AddDays(-2);

        var store = new InMemoryFoodDiaryStore();

        var firstMeal = CreateMeal(startDate);

        var lastMeal = CreateMeal(endDate);

        store.SaveMeal(firstMeal);

        store.SaveMeal(lastMeal);

        store.SaveFoodEntry(
            CreateFood(
                firstMeal.Id,
                "Первый день",
                500m
            )
        );

        store.SaveFoodEntry(
            CreateFood(
                lastMeal.Id,
                "Последний день",
                700m
            )
        );

        store.SetDateComplete(
            endDate,
            true
        );

        var provider = new FoodDiaryDaySnapshotProvider(store);

        var result = provider.GetRange(
            startDate,
            endDate
        );

        Assert.Equal(
            3,
            result.Count
        );

        Assert.Equal(
            startDate,
            result[0].Date
        );

        Assert.Equal(
            500m,
            result[0].ConsumedTotals.CaloriesKcal
        );

        Assert.Equal(
            0m,
            result[1].ConsumedTotals.CaloriesKcal
        );

        Assert.Empty(result[1].Meals);

        Assert.Equal(
            700m,
            result[2].ConsumedTotals.CaloriesKcal
        );

        Assert.True(result[2].IsComplete);
    }

    [Fact]
    public void GetDay_ApproximateFood_PreservesItemMetadata() {
        var date = new DateOnly(2026, 8, 17);

        var store = new InMemoryFoodDiaryStore();

        var meal = CreateMeal(date);

        store.SaveMeal(meal);

        store.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: meal.Id,
                Name: "Ресторан",
                Quantity: FoodQuantity.Portions(1m),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Total,
                    CaloriesKcal: 1200m,
                    ProteinG: null,
                    FatG: null,
                    CarbsG: null
                ),
                Source: FoodLogSource.Approximation,
                IsApproximate: true,
                Note: "Приблизительно"
            )
        );

        var provider = new FoodDiaryDaySnapshotProvider(store);

        var result = provider.GetDay(date);

        var food = Assert.Single(
            Assert.Single(result.Meals).FoodItems
        );

        Assert.Equal(
            "Ресторан",
            food.Name
        );

        Assert.True(food.IsApproximate);

        Assert.Equal(
            "Приблизительно",
            food.Note
        );

        Assert.Equal(
            1200m,
            food.Totals.CaloriesKcal
        );

        Assert.Null(food.Totals.ProteinG);
    }

    [Fact]
    public void GetRange_EndBeforeStart_Throws() {
        var provider = new FoodDiaryDaySnapshotProvider(
            new InMemoryFoodDiaryStore()
        );

        Assert.Throws<ArgumentOutOfRangeException>(
            () => provider.GetRange(
                new DateOnly(2026, 8, 17),
                new DateOnly(2026, 8, 16)
            )
        );
    }

    private static MealEntry CreateMeal(DateOnly date) {
        return new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Другое",
            Role: MealGroupRole.Custom
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
                ProteinG: 20m,
                FatG: 20m,
                CarbsG: 50m
            ),
            Source: FoodLogSource.Manual
        );
    }
}
