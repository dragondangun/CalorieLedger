using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Today;

public sealed class SampleTodayDashboardSnapshotProvider(
    IUserNutritionProfileProvider profileProvider)
    :ITodayDashboardSnapshotProvider {
    public TodayDashboardSnapshot GetToday() {
        var profile = profileProvider.GetCurrentProfile();
        var target = NutritionTargetCalculator.Calculate(profile);

        var consumedTotals = new NutritionTotals(
            CaloriesKcal: 1350m,
            ProteinG: 82m,
            FatG: 48m,
            CarbsG: 145m);

        return new TodayDashboardSnapshot(
            Target: target,
            ConsumedTotals: consumedTotals);
    }
}