using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Nutrition;

public sealed class DailyEnergyIntakeHistoryProvider:IDailyEnergyIntakeHistoryProvider {
    private readonly FoodDiaryDaySnapshotProvider snapshotProvider;
    private readonly IActivityStore? activityStore;

    public DailyEnergyIntakeHistoryProvider(
        IFoodDiaryStore foodDiaryStore,
        IActivityStore? activityStore = null
    ) : this(new FoodDiaryDaySnapshotProvider(foodDiaryStore), activityStore) { }

    public DailyEnergyIntakeHistoryProvider(
        FoodDiaryDaySnapshotProvider snapshotProvider,
        IActivityStore? activityStore = null
    ) {
        ArgumentNullException.ThrowIfNull(snapshotProvider);

        this.snapshotProvider = snapshotProvider;
        this.activityStore = activityStore;
    }

    public IReadOnlyList<DailyEnergyIntakeEntry> GetEntries(DateOnly startDate, DateOnly endDate) {
        Dictionary<DateOnly, decimal> activityByDate = activityStore is null
        ? []
        : activityStore.Get(startDate, endDate)
            .GroupBy(entry => entry.Date)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(entry => entry.BurnedCaloriesKcal)
            );

        return [
            .. snapshotProvider.GetRange(startDate, endDate)
            .Select(day => new DailyEnergyIntakeEntry(
                Date: day.Date,
                CaloriesKcal: day.ConsumedTotals.CaloriesKcal ?? 0m,
                IsComplete: day.IsEnergyComplete,
                ExtraActivityBurnedCaloriesKcal: activityByDate.GetValueOrDefault(day.Date))
            )
        ];
    }
}
