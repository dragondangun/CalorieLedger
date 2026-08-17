using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Nutrition;

public sealed class DailyEnergyIntakeHistoryProvider:IDailyEnergyIntakeHistoryProvider {
    private readonly FoodDiaryDaySnapshotProvider snapshotProvider;

    public DailyEnergyIntakeHistoryProvider(IFoodDiaryStore foodDiaryStore) : this(
        new FoodDiaryDaySnapshotProvider(foodDiaryStore)
    ) { }

    public DailyEnergyIntakeHistoryProvider(FoodDiaryDaySnapshotProvider snapshotProvider) {
        ArgumentNullException.ThrowIfNull(snapshotProvider);

        this.snapshotProvider = snapshotProvider;
    }

    public IReadOnlyList<DailyEnergyIntakeEntry> GetEntries(
        DateOnly startDate,
        DateOnly endDate
    ) {
        return [
            .. snapshotProvider
                .GetRange(startDate, endDate)
                .Select(
                    day => new DailyEnergyIntakeEntry(
                        Date: day.Date,
                        CaloriesKcal: day.ConsumedTotals.CaloriesKcal ?? 0m,
                        IsComplete: day.IsEnergyComplete
                    )
                ),
        ];
    }
}
