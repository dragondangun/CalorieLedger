using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Today;

public sealed class SampleTodayDashboardSnapshotProvider(
    IUserNutritionProfileProvider profileProvider)
    :ITodayDashboardSnapshotProvider {
    public TodayDashboardSnapshot GetToday() {
        var profile = profileProvider.GetCurrentProfile();
        var target = NutritionTargetCalculator.Calculate(profile);

        var foodItems = new[]
        {
            new TodayFoodLogSnapshotItem(
                Name: "Тестовый завтрак",
                Quantity: FoodQuantity.Portions(1m),
                Totals: new NutritionTotals(
                    CaloriesKcal: 620m,
                    ProteinG: 35m,
                    FatG: 22m,
                    CarbsG: 70m)),

            new TodayFoodLogSnapshotItem(
                Name: "Тестовый обед",
                Quantity: FoodQuantity.Portions(1m),
                Totals: new NutritionTotals(
                    CaloriesKcal: 730m,
                    ProteinG: 47m,
                    FatG: 26m,
                    CarbsG: 75m))
        };

        var consumedTotals = new NutritionTotals(
            CaloriesKcal: foodItems.Sum(x => x.Totals.CaloriesKcal ?? 0m),
            ProteinG: foodItems.Sum(x => x.Totals.ProteinG ?? 0m),
            FatG: foodItems.Sum(x => x.Totals.FatG ?? 0m),
            CarbsG: foodItems.Sum(x => x.Totals.CarbsG ?? 0m));

        return new TodayDashboardSnapshot(
            Target: target,
            ConsumedTotals: consumedTotals,
            FoodItems: foodItems);
    }
}