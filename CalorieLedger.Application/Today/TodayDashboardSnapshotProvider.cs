using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Time;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Today;

public sealed class TodayDashboardSnapshotProvider:ITodayDashboardSnapshotProvider {
    private const int WeeklyDayCount = 7;

    private readonly IUserNutritionProfileProvider profileProvider;
    private readonly IFoodDiaryStore foodDiaryStore;
    private readonly ICurrentDateProvider currentDateProvider;

    public TodayDashboardSnapshotProvider(
        IUserNutritionProfileProvider profileProvider,
        IFoodDiaryStore foodDiaryStore,
        ICurrentDateProvider currentDateProvider
    ) {
        ArgumentNullException.ThrowIfNull(profileProvider);
        ArgumentNullException.ThrowIfNull(foodDiaryStore);
        ArgumentNullException.ThrowIfNull(currentDateProvider);

        this.profileProvider = profileProvider;
        this.foodDiaryStore = foodDiaryStore;
        this.currentDateProvider = currentDateProvider;
    }

    public TodayDashboardSnapshot GetToday() {
        var currentDate = currentDateProvider.GetCurrentDate();

        var weekStartDate = currentDate.AddDays(-(WeeklyDayCount - 1));

        var profile = profileProvider.GetCurrentProfile();

        var target = NutritionTargetCalculator.Calculate(profile);

        var goalDecision = NutritionGoalDecisionEvaluator.Evaluate(
            profile.Body,
            profile.Goal
        );

        var meals = foodDiaryStore.GetMeals(
            weekStartDate,
            currentDate
        );

        var foodEntries = foodDiaryStore.GetFoodEntries(
            [
                .. meals.Select(
                    meal => meal.Id
                ),
            ]
        );

        var mealsByDate = meals.ToLookup(meal => meal.Date);

        var foodEntriesByMealId = foodEntries.ToLookup(foodEntry => foodEntry.MealEntryId);

        IReadOnlyList<TodayMealSnapshot> todayMeals = [
            .. mealsByDate[currentDate].Select(
                meal => new TodayMealSnapshot(
                    Name: meal.Name,
                    Role: meal.Role,
                    EatenAt: meal.EatenAt,
                    FoodItems: [
                        .. foodEntriesByMealId[meal.Id].Select(
                            CreateFoodItem
                        ),
                    ]
                )
            ),
        ];

        var consumedTotals = CalculateTotals(
            mealsByDate[currentDate].SelectMany(meal => foodEntriesByMealId[meal.Id])
        );

        var weeklySummary = new WeeklyNutritionSummarySnapshot(
            [
                .. Enumerable.Range(
                    0,
                    WeeklyDayCount
                ).Select(
                    day => {
                        var date = weekStartDate.AddDays(day);

                        return new DailyNutritionSummarySnapshot(
                            Date: date,
                            ConsumedTotals: CalculateTotals(mealsByDate[date].SelectMany(meal => foodEntriesByMealId[meal.Id]))
                        );
                    }
                ),
            ]
        );

        var isFoodLogComplete = foodDiaryStore
            .GetCompletedDates(currentDate, currentDate)
            .Contains(currentDate);

        return new TodayDashboardSnapshot(
            Target: target,
            ConsumedTotals: consumedTotals,
            Meals: todayMeals,
            WeeklySummary: weeklySummary,
            Activities: [],
            GoalDecision: goalDecision,
            IsFoodLogComplete: isFoodLogComplete
        );
    }

    private static TodayFoodLogSnapshotItem CreateFoodItem(FoodLogEntry foodEntry) {
        return new TodayFoodLogSnapshotItem(
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
            CaloriesKcal: caloriesKcal,
            ProteinG: proteinG,
            FatG: fatG,
            CarbsG: carbsG
        );
    }
}
