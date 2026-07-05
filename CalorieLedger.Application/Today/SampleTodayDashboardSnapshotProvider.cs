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

        return new TodayDashboardSnapshot(
            Target: target,
            ConsumedTotals: consumedTotals,
            Meals: meals);
    }
}