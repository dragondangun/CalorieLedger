using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Today;

public sealed class SampleTodayDashboardSnapshotProvider(
    IUserNutritionProfileProvider profileProvider)
    :ITodayDashboardSnapshotProvider {
    public TodayDashboardSnapshot GetToday() {
        var profile = profileProvider.GetCurrentProfile();
        var target = NutritionTargetCalculator.Calculate(profile);

        var meals = new[]
        {
            new TodayMealSnapshot(
                Name: "Завтрак",
                Role: MealGroupRole.Breakfast,
                EatenAt: new TimeOnly(9, 0),
                FoodItems:
                [
                    new TodayFoodLogSnapshotItem(
                        Name: "Тестовый завтрак",
                        Quantity: FoodQuantity.Portions(1m),
                        Totals: new NutritionTotals(
                            CaloriesKcal: 620m,
                            ProteinG: 35m,
                            FatG: 22m,
                            CarbsG: 70m))
                ]),

            new TodayMealSnapshot(
                Name: "Обед",
                Role: MealGroupRole.Lunch,
                EatenAt: new TimeOnly(14, 0),
                FoodItems:
                [
                    new TodayFoodLogSnapshotItem(
                        Name: "Тестовый обед",
                        Quantity: FoodQuantity.Portions(1m),
                        Totals: new NutritionTotals(
                            CaloriesKcal: 730m,
                            ProteinG: 47m,
                            FatG: 26m,
                            CarbsG: 75m))
                ]),

            new TodayMealSnapshot(
                Name: "Перекусы",
                Role: MealGroupRole.Snack,
                EatenAt: null,
                FoodItems: [])
        };

        var consumedTotals = new NutritionTotals(
            CaloriesKcal: meals.SelectMany(x => x.FoodItems).Sum(x => x.Totals.CaloriesKcal ?? 0m),
            ProteinG: meals.SelectMany(x => x.FoodItems).Sum(x => x.Totals.ProteinG ?? 0m),
            FatG: meals.SelectMany(x => x.FoodItems).Sum(x => x.Totals.FatG ?? 0m),
            CarbsG: meals.SelectMany(x => x.FoodItems).Sum(x => x.Totals.CarbsG ?? 0m));

        var weeklySummary = new WeeklyNutritionSummarySnapshot(
            [
                new DailyNutritionSummarySnapshot(
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-6)),
                    new NutritionTotals(2050m, 125m, 70m, 230m)),

                new DailyNutritionSummarySnapshot(
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                    new NutritionTotals(2180m, 132m, 74m, 245m)),

                new DailyNutritionSummarySnapshot(
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-4)),
                    new NutritionTotals(2350m, 140m, 82m, 260m)),

                new DailyNutritionSummarySnapshot(
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
                    new NutritionTotals(1980m, 118m, 65m, 220m)),

                new DailyNutritionSummarySnapshot(
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
                    new NutritionTotals(2250m, 136m, 76m, 250m)),

                new DailyNutritionSummarySnapshot(
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    new NutritionTotals(2100m, 128m, 72m, 235m)),

                new DailyNutritionSummarySnapshot(
                    DateOnly.FromDateTime(DateTime.Today),
                    consumedTotals)
            ]);

        return new TodayDashboardSnapshot(
            Target: target,
            ConsumedTotals: consumedTotals,
            Meals: meals,
            WeeklySummary: weeklySummary);
    }
}