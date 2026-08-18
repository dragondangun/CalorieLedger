using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Time;
using CalorieLedger.Application.Today;
using CalorieLedger.Domain.Activities;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.Today;

public sealed class TodayDashboardSnapshotProviderTests {
    [Fact]
    public void GetToday_DiaryHistory_BuildsTodayAndWeeklySummary() {
        var currentDate = new DateOnly(2026, 8, 10);

        var store = new InMemoryFoodDiaryStore();

        var yesterdayMeal = CreateMeal(
            currentDate.AddDays(-1),
            "Ужин"
        );

        var todayMeal = CreateMeal(
            currentDate,
            "Перекусы"
        );

        store.SaveMeal(yesterdayMeal);

        store.SaveMeal(todayMeal);

        store.SaveFoodEntry(
            CreateFoodEntry(
                yesterdayMeal.Id,
                "Вчера",
                600m
            )
        );

        store.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: todayMeal.Id,
                Name: "Творог",
                Quantity: FoodQuantity.Grams(
                    250m
                ),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Per100Grams,
                    CaloriesKcal: 120m,
                    ProteinG: 17m,
                    FatG: 5m,
                    CarbsG: 3m
                ),
                Source: FoodLogSource.Manual
            )
        );

        store.SetDateComplete(
            currentDate,
            true
        );

        var provider = new TodayDashboardSnapshotProvider(
            new SampleUserNutritionProfileProvider(),
            new FoodDiaryDaySnapshotProvider(store),
            new InMemoryActivityStore(),
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetToday();

        var todayMealResult = Assert.Single(result.Meals);

        Assert.Equal(
            "Перекусы",
            todayMealResult.Name
        );

        Assert.Equal(
            300m,
            result.ConsumedTotals.CaloriesKcal
        );

        Assert.Equal(
            7,
            result.WeeklySummary.Days.Count
        );

        Assert.Equal(
            currentDate.AddDays(-6),
            result.WeeklySummary.Days[0].Date
        );

        Assert.Equal(
            currentDate,
            result.WeeklySummary.Days[^1].Date
        );

        Assert.Equal(
            600m,
            result.WeeklySummary.Days[^2]
                .ConsumedTotals
                .CaloriesKcal
        );

        Assert.Equal(
            300m,
            result.WeeklySummary.Days[^1]
                .ConsumedTotals
                .CaloriesKcal
        );

        Assert.Equal(
            1,
            result.WeeklySummary.EnergyCompleteDayCount
        );

        Assert.Equal(
            1,
            result.WeeklySummary.MacroCompleteDayCount
        );

        Assert.Equal(
            300m,
            result.WeeklySummary.AverageCaloriesKcal
        );

        Assert.True(result.IsFoodLogComplete);

        Assert.Empty(result.Activities);
    }

    [Fact]
    public void GetToday_EmptyDiary_ReturnsZeroTotalsAndSevenDays() {
        var currentDate = new DateOnly(2026, 8, 10);

        var provider = new TodayDashboardSnapshotProvider(
            new SampleUserNutritionProfileProvider(),
            new FoodDiaryDaySnapshotProvider(new InMemoryFoodDiaryStore()),
            new InMemoryActivityStore(),
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetToday();

        Assert.Empty(
            result.Meals
        );

        Assert.Equal(
            0m,
            result.ConsumedTotals.CaloriesKcal
        );

        Assert.Equal(
            7,
            result.WeeklySummary.Days.Count
        );

        Assert.False(result.IsFoodLogComplete);
    }

    [Fact]
    public void GetToday_ApproximateFood_PreservesApproximationMetadata() {
        var currentDate = new DateOnly(2026, 8, 10);

        var store = new InMemoryFoodDiaryStore();

        var meal = CreateMeal(
            currentDate,
            "Особые события"
        );

        store.SaveMeal(meal);

        store.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: meal.Id,
                Name: "Праздник",
                Quantity: FoodQuantity.Portions(
                    1m
                ),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Total,
                    CaloriesKcal: 1500m,
                    ProteinG: null,
                    FatG: null,
                    CarbsG: null
                ),
                Source: FoodLogSource.Approximation,
                IsApproximate: true,
                Note: "Оценка"
            )
        );

        var provider = new TodayDashboardSnapshotProvider(
            new SampleUserNutritionProfileProvider(),
            new FoodDiaryDaySnapshotProvider(store),
            new InMemoryActivityStore(),
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetToday();

        var food = Assert.Single(
            Assert.Single(
                result.Meals
            ).FoodItems
        );

        Assert.True(
            food.IsApproximate
        );

        Assert.Equal(
            "Оценка",
            food.Note
        );

        Assert.Equal(
            1500m,
            food.Totals.CaloriesKcal
        );

        Assert.Null(
            food.Totals.ProteinG
        );
    }

    [Fact]
    public void GetToday_ActivityStore_ReturnsOnlyCurrentDateActivities() {
        var currentDate = new DateOnly(2026, 8, 18);

        var activityStore = new InMemoryActivityStore();

        activityStore.Save(
            new ActivityEntry(
                Id: Guid.NewGuid(),
                Date: currentDate.AddDays(-1),
                Name: "Вчера",
                BurnedCaloriesKcal: 200m
            )
        );

        var todayActivityId = Guid.NewGuid();

        activityStore.Save(
            new ActivityEntry(
                Id: todayActivityId,
                Date: currentDate,
                Name: "HEMA",
                BurnedCaloriesKcal: 350m,
                StartedAt: new TimeOnly(18, 30),
                Duration: TimeSpan.FromMinutes(75),
                Note: "Тренировка"
            )
        );

        var provider = new TodayDashboardSnapshotProvider(
            new SampleUserNutritionProfileProvider(),
            new FoodDiaryDaySnapshotProvider(new InMemoryFoodDiaryStore()),
            activityStore,
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetToday();

        var activity = Assert.Single(result.Activities);

        Assert.Equal(
            todayActivityId,
            activity.Id
        );

        Assert.Equal(
            "HEMA",
            activity.Name
        );

        Assert.Equal(
            350m,
            activity.BurnedCaloriesKcal
        );

        Assert.Equal(
            new TimeOnly(18, 30),
            activity.StartedAt
        );

        Assert.Equal(
            TimeSpan.FromMinutes(75),
            activity.Duration
        );

        Assert.Equal(
            "Тренировка",
            activity.Note
        );
    }

    private static MealEntry CreateMeal(DateOnly date, string name) {
        return new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: name,
            Role: MealGroupRole.Custom
        );
    }

    private static FoodLogEntry CreateFoodEntry(
        Guid mealId,
        string name,
        decimal caloriesKcal
    ) {
        return new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: mealId,
            Name: name,
            Quantity: FoodQuantity.Portions(
                1m
            ),
            Nutrition:
                new NutritionFacts(
                    Basis: NutritionBasis.Total,
                    CaloriesKcal: caloriesKcal,
                    ProteinG: 30m,
                    FatG: 20m,
                    CarbsG: 50m
                ),
            Source: FoodLogSource.Manual
        );
    }

    private sealed class FixedCurrentDateProvider:ICurrentDateProvider {
        private readonly DateOnly currentDate;

        public FixedCurrentDateProvider(DateOnly currentDate) {
            this.currentDate = currentDate;
        }

        public DateOnly GetCurrentDate() {
            return currentDate;
        }
    }
}
