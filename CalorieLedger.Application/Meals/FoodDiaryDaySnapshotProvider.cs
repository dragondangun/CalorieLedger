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
                        startDate.AddDays(
                            offset
                        ),
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
                        .. foodEntriesByMealId[meal.Id].Select(CreateFoodItem),
                    ]
                )
            ),
        ];

        var nutrition = CalculateNutrition(meals.SelectMany(meal => foodEntriesByMealId[meal.Id]));

        return new FoodDiaryDaySnapshot(
            Date: date,
            Meals: mealSnapshots,
            ConsumedTotals: nutrition.Totals,
            IsComplete: completedDates.Contains(date),
            HasUnknownCalories: nutrition.HasUnknownCalories,
            HasUnknownProtein: nutrition.HasUnknownProtein,
            HasUnknownFat: nutrition.HasUnknownFat,
            HasUnknownCarbs: nutrition.HasUnknownCarbs
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

    private static (
        NutritionTotals Totals,
        bool HasUnknownCalories,
        bool HasUnknownProtein,
        bool HasUnknownFat,
        bool HasUnknownCarbs
    ) CalculateNutrition(IEnumerable<FoodLogEntry> foodEntries) {
        var caloriesKcal = 0m;
        var proteinG = 0m;
        var fatG = 0m;
        var carbsG = 0m;

        var hasUnknownCalories = false;
        var hasUnknownProtein = false;
        var hasUnknownFat = false;
        var hasUnknownCarbs = false;

        foreach(var foodEntry in foodEntries) {
            var totals = NutritionCalculator.CalculateTotal(
                foodEntry.Nutrition,
                foodEntry.Quantity
            );

            if(totals.CaloriesKcal is decimal calories) {
                caloriesKcal += calories;
            }
            else {
                hasUnknownCalories = true;
            }

            if(totals.ProteinG is decimal protein) {
                proteinG += protein;
            }
            else {
                hasUnknownProtein = true;
            }

            if(totals.FatG is decimal fat) {
                fatG += fat;
            }
            else {
                hasUnknownFat = true;
            }

            if(totals.CarbsG is decimal carbs) {
                carbsG += carbs;
            }
            else {
                hasUnknownCarbs = true;
            }
        }

        return (
            Totals: new NutritionTotals(
                CaloriesKcal: caloriesKcal,
                ProteinG: proteinG,
                FatG: fatG,
                CarbsG: carbsG
            ),
            HasUnknownCalories: hasUnknownCalories,
            HasUnknownProtein: hasUnknownProtein,
            HasUnknownFat: hasUnknownFat,
            HasUnknownCarbs: hasUnknownCarbs
        );
    }
}
