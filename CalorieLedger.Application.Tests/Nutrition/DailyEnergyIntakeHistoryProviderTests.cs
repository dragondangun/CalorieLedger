using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Nutrition;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.Nutrition;

public sealed class DailyEnergyIntakeHistoryProviderTests {
    [Fact]
    public void GetEntries_CompleteDay_SumsFoodAcrossMeals() {
        var date = new DateOnly(2026, 8, 10);

        var store = new InMemoryFoodDiaryStore();

        var breakfast = CreateMeal(date, "Завтрак");

        var dinner = CreateMeal(date, "Ужин");

        store.SaveMeal(breakfast);

        store.SaveMeal(dinner);

        store.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: breakfast.Id,
                Name: "Творог",
                Quantity: FoodQuantity.Grams(250m),
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

        store.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: dinner.Id,
                Name: "Ужин",
                Quantity: FoodQuantity.Portions(1m),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Total,
                    CaloriesKcal: 700m,
                    ProteinG: 40m,
                    FatG: 20m,
                    CarbsG: 80m
                ),
                Source: FoodLogSource.Manual
            )
        );

        store.SetDateComplete(date, true);

        var provider = new DailyEnergyIntakeHistoryProvider(store);

        var entry = Assert.Single(
            provider.GetEntries(
                date,
                date
            )
        );

        Assert.Equal(
            1000m,
            entry.CaloriesKcal
        );

        Assert.True(entry.IsComplete);
    }

    [Fact]
    public void GetEntries_CompletedDayWithUnknownCalories_MarksDayIncomplete() {
        var date = new DateOnly(2026, 8, 10);

        var store = new InMemoryFoodDiaryStore();

        var meal = CreateMeal(
            date,
            "Обед"
        );

        store.SaveMeal(meal);

        store.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: meal.Id,
                Name: "Неизвестное блюдо",
                Quantity: FoodQuantity.Grams(200m),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Per100Grams,
                    CaloriesKcal: null,
                    ProteinG: null,
                    FatG: null,
                    CarbsG: null
                ),
                Source: FoodLogSource.Approximation,
                IsApproximate: true
            )
        );

        store.SetDateComplete(date, true);

        var provider = new DailyEnergyIntakeHistoryProvider(store);

        var entry = Assert.Single(
            provider.GetEntries(
                date,
                date
            )
        );

        Assert.False(entry.IsComplete);
    }

    [Fact]
    public void GetEntries_Range_ReturnsEveryDateInChronologicalOrder() {
        var startDate = new DateOnly(2026, 8, 10);

        var endDate = startDate.AddDays(2);

        var store = new InMemoryFoodDiaryStore();

        store.SetDateComplete(startDate, true);

        store.SetDateComplete(endDate, true);

        var provider = new DailyEnergyIntakeHistoryProvider(store);

        var entries = provider.GetEntries(
            startDate,
            endDate
        );

        Assert.Equal(
            3,
            entries.Count
        );

        Assert.Equal(
            startDate,
            entries[0].Date
        );

        Assert.Equal(
            startDate.AddDays(1),
            entries[1].Date
        );

        Assert.Equal(endDate, entries[2].Date);

        Assert.True(entries[0].IsComplete);

        Assert.False(entries[1].IsComplete);

        Assert.True(entries[2].IsComplete);
    }

    [Fact]
    public void GetEntries_CompletedEmptyDay_ReturnsZeroCaloriesAsComplete() {
        var date = new DateOnly(2026, 8, 10);

        var store = new InMemoryFoodDiaryStore();

        store.SetDateComplete(date, true);

        var provider = new DailyEnergyIntakeHistoryProvider(store);

        var entry = Assert.Single(
            provider.GetEntries(
                date,
                date
            )
        );

        Assert.Equal(0m, entry.CaloriesKcal);

        Assert.True(entry.IsComplete);
    }

    private static MealEntry CreateMeal(
        DateOnly date,
        string name
    ) {
        return new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: name,
            Role: MealGroupRole.Custom
        );
    }
}
