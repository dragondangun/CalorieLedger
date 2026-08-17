using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Meals;

public sealed class FoodDiaryDaySnapshotProvider {
    private readonly IFoodDiaryStore foodDiaryStore;

    public FoodDiaryDaySnapshotProvider(IFoodDiaryStore foodDiaryStore) {
        ArgumentNullException.ThrowIfNull(foodDiaryStore);

        this.foodDiaryStore = foodDiaryStore;
    }

    public FoodDiaryDaySnapshot GetDay(DateOnly date) {
        return GetRange(date, date)[0];
    }

    public IReadOnlyList<FoodDiaryDaySnapshot> GetRange(DateOnly startDate, DateOnly endDate) {
        if(endDate < startDate) {
            throw new ArgumentOutOfRangeException(
                nameof(endDate),
                endDate,
                "End date cannot be earlier than start date."
            );
        }

        var meals = foodDiaryStore.GetMeals(startDate, endDate);

        IReadOnlyCollection<Guid> mealIds = [
            .. meals.Select(meal => meal.Id),
        ];

        var foodEntries = foodDiaryStore.GetFoodEntries(mealIds);

        var completedDates = foodDiaryStore.GetCompletedDates(startDate, endDate).ToHashSet();

        var mealsByDate = meals.ToLookup(meal => meal.Date);

        var foodEntriesByMealId = foodEntries.ToLookup(foodEntry => foodEntry.MealEntryId);

        var dayCount = endDate.DayNumber - startDate.DayNumber + 1;

        return [
            .. Enumerable
                .Range(0, dayCount)
                .Select(
                    offset => CreateDaySnapshot(
                        startDate.AddDays(offset),
                        mealsByDate,
                        foodEntriesByMealId,
                        completedDates
                    )
                ),
        ];
    }

    private static FoodDiaryDaySnapshot CreateDaySnapshot(
        DateOnly date,
        ILookup<DateOnly, MealEntry> mealsByDate,
        ILookup<Guid, FoodLogEntry> foodEntriesByMealId,
        IReadOnlySet<DateOnly> completedDates
    ) {
        var meals = mealsByDate[date];

        IReadOnlyList<FoodDiaryMealSnapshot> mealSnapshots = [
            .. meals.Select(
                meal => new FoodDiaryMealSnapshot(
                    Name: meal.Name,
                    Role: meal.Role,
                    EatenAt: meal.EatenAt,
                    FoodItems: [
                        .. foodEntriesByMealId[meal.Id]
                            .Select(CreateFoodItem),
                    ]
                )
            ),
        ];

        var consumedTotals = CalculateTotals(meals.SelectMany(meal => foodEntriesByMealId[meal.Id]));

        return new FoodDiaryDaySnapshot(
            Date: date,
            Meals: mealSnapshots,
            ConsumedTotals: consumedTotals,
            IsComplete: completedDates.Contains(date)
        );
    }

    private static FoodDiaryFoodSnapshotItem CreateFoodItem(FoodLogEntry foodEntry) {
        return new FoodDiaryFoodSnapshotItem(
            Id: foodEntry.Id,
            Name: foodEntry.Name,
            Quantity: foodEntry.Quantity,
            Totals: NutritionCalculator.CalculateTotal(
                foodEntry.Nutrition,
                foodEntry.Quantity
            ),
            IsApproximate: foodEntry.IsApproximate,
            Note: foodEntry.Note
        );
    }

    private static NutritionTotals CalculateTotals(IEnumerable<FoodLogEntry> foodEntries) {
        var caloriesKcal = 0m;
        var proteinG = 0m;
        var fatG = 0m;
        var carbsG = 0m;

        foreach(var foodEntry in foodEntries) {
            var totals = NutritionCalculator.CalculateTotal(
                foodEntry.Nutrition,
                foodEntry.Quantity
            );

            caloriesKcal += totals.CaloriesKcal ?? 0m;
            proteinG += totals.ProteinG ?? 0m;
            fatG += totals.FatG ?? 0m;
            carbsG += totals.CarbsG ?? 0m;
        }

        return new NutritionTotals(
            CaloriesKcal:caloriesKcal,
            ProteinG: proteinG,
            FatG: fatG,
            CarbsG: carbsG
        );
    }
}
