using CalorieLedger.Application.Activities;
using CalorieLedger.Application.History;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Activities;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.History;

public sealed class WeeklyJournalSummaryProviderTests {
    [Fact]
    public void GetWeek_UsesCompleteFoodDaysAndCombinesActivityAndWeight() {
        var currentDate = new DateOnly(2026, 8, 19);
        var monday = new DateOnly(2026, 8, 17);
        var tuesday = monday.AddDays(1);

        var foodStore = new InMemoryFoodDiaryStore();
        var activityStore = new InMemoryActivityStore();
        var bodyStore = new InMemoryBodyMeasurementStore();

        AddFood(foodStore, monday, 2000m, isComplete: true);
        AddFood(foodStore, tuesday, 2200m, isComplete: true);
        AddFood(foodStore, currentDate, 1500m, isComplete: false);

        activityStore.Save(
            new ActivityEntry(
                Id: Guid.NewGuid(),
                Date: monday,
                Name: "HEMA",
                BurnedCaloriesKcal: 300m
            )
        );

        activityStore.Save(
            new ActivityEntry(
                Id: Guid.NewGuid(),
                Date: tuesday,
                Name: "Ходьба",
                BurnedCaloriesKcal: 100m
            )
        );

        activityStore.Save(
            new ActivityEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                Name: "Прогулка",
                BurnedCaloriesKcal: 200m
            )
        );

        bodyStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: monday,
                WeightKg: 60.0m
            )
        );

        bodyStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                WeightKg: 59.7m
            )
        );

        var provider = CreateProvider(foodStore, activityStore, bodyStore);
        var result = provider.GetWeek(currentDate, currentDate);

        Assert.Equal(3, result.AvailableDayCount);
        Assert.Equal(2, result.EnergyCompleteDayCount);
        Assert.Equal(2, result.MacroCompleteDayCount);

        Assert.Equal(2100m, result.AverageFoodCaloriesKcal);
        Assert.Equal(200m, result.AverageExtraActivityBurnedCaloriesKcal);
        Assert.Equal(1900m, result.AverageActivityAdjustedCaloriesKcal);
        Assert.Equal(600m, result.TotalExtraActivityBurnedCaloriesKcal);

        Assert.Equal(2, result.WeightMeasurementCount);
        Assert.Equal(60.0m, result.FirstWeightKg);
        Assert.Equal(59.7m, result.LastWeightKg);
        Assert.Equal(-0.3m, result.WeightChangeKg);
    }

    [Fact]
    public void GetWeek_NoCompleteFoodDaysOrWeightPair_ReturnsUnavailableAveragesAndChange() {
        var currentDate = new DateOnly(2026, 8, 19);
        var foodStore = new InMemoryFoodDiaryStore();
        var activityStore = new InMemoryActivityStore();
        var bodyStore = new InMemoryBodyMeasurementStore();

        AddFood(foodStore, currentDate, 1500m, isComplete: false);

        bodyStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                WeightKg: 60m
            )
        );

        var provider = CreateProvider(foodStore, activityStore, bodyStore);
        var result = provider.GetWeek(currentDate, currentDate);

        Assert.Equal(0, result.EnergyCompleteDayCount);
        Assert.Null(result.AverageFoodCaloriesKcal);
        Assert.Null(result.AverageExtraActivityBurnedCaloriesKcal);
        Assert.Null(result.AverageActivityAdjustedCaloriesKcal);

        Assert.Equal(1, result.WeightMeasurementCount);
        Assert.Equal(60m, result.FirstWeightKg);
        Assert.Equal(60m, result.LastWeightKg);
        Assert.Null(result.WeightChangeKg);
    }

    [Fact]
    public void GetRecentWeeks_ReturnsChronologicalWeeklySummaries() {
        var currentDate = new DateOnly(2026, 8, 19);
        var selectedWeek = new DateOnly(2026, 8, 17);
        var previousWeek = selectedWeek.AddDays(-7);
        var olderWeek = selectedWeek.AddDays(-14);

        var foodStore = new InMemoryFoodDiaryStore();
        var activityStore = new InMemoryActivityStore();
        var bodyStore = new InMemoryBodyMeasurementStore();

        AddFood(foodStore, olderWeek, 1800m, isComplete: true);
        AddFood(foodStore, previousWeek, 2000m, isComplete: true);
        AddFood(foodStore, selectedWeek, 2200m, isComplete: true);

        activityStore.Save(
            new ActivityEntry(
                Id: Guid.NewGuid(),
                Date: previousWeek,
                Name: "HEMA",
                BurnedCaloriesKcal: 300m
            )
        );

        activityStore.Save(
            new ActivityEntry(
                Id: Guid.NewGuid(),
                Date: selectedWeek,
                Name: "Ходьба",
                BurnedCaloriesKcal: 100m
            )
        );

        var provider = CreateProvider(foodStore, activityStore, bodyStore);

        var result = provider.GetRecentWeeks(
        currentDate,
        currentDate,
        3
    );

        Assert.Equal(3, result.Count);

        Assert.Equal(olderWeek, result[0].WeekStartDate);
        Assert.Equal(previousWeek, result[1].WeekStartDate);
        Assert.Equal(selectedWeek, result[2].WeekStartDate);

        Assert.Equal(1800m, result[0].AverageActivityAdjustedCaloriesKcal);
        Assert.Equal(1700m, result[1].AverageActivityAdjustedCaloriesKcal);
        Assert.Equal(2100m, result[2].AverageActivityAdjustedCaloriesKcal);
    }

    [Fact]
    public void GetRecentWeeks_CurrentWeek_UsesOnlyElapsedDays() {
        var currentDate = new DateOnly(2026, 8, 19);
        var currentWeekStart = new DateOnly(2026, 8, 17);

        var provider = CreateProvider(
            new InMemoryFoodDiaryStore(),
            new InMemoryActivityStore(),
            new InMemoryBodyMeasurementStore()
        );

        var result = provider.GetRecentWeeks(
            currentDate,
            currentDate,
            2
        );

        Assert.Equal(2, result.Count);

        var previous = result[0];
        Assert.Equal(7, previous.AvailableDayCount);

        var current = result[1];
        Assert.Equal(currentWeekStart, current.WeekStartDate);
        Assert.Equal(currentDate, current.AvailableEndDate);
        Assert.Equal(3, current.AvailableDayCount);
    }

    private static WeeklyJournalSummaryProvider CreateProvider(
        IFoodDiaryStore foodStore,
        IActivityStore activityStore,
        IBodyMeasurementStore bodyStore
    ) {
        var journalProvider = new DailyJournalDaySnapshotProvider(
            new FoodDiaryDaySnapshotProvider(foodStore),
            activityStore
        );

        return new WeeklyJournalSummaryProvider(
            journalProvider,
            new BodyMeasurementHistoryService(bodyStore)
        );
    }

    private static void AddFood(
        IFoodDiaryStore store,
        DateOnly date,
        decimal caloriesKcal,
        bool isComplete
    ) {
        var meal = new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Другое",
            Role: MealGroupRole.Custom
        );

        store.SaveMeal(meal);

        store.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: meal.Id,
                Name: "Еда",
                Quantity: FoodQuantity.Portions(1m),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Total,
                    CaloriesKcal: caloriesKcal,
                    ProteinG: 100m,
                    FatG: 60m,
                    CarbsG: 200m
                ),
                Source: FoodLogSource.Manual
            )
        );

        if(isComplete) {
            store.SetDateComplete(date, true);
        }
    }
}
