using CalorieLedger.Application.Activities;
using CalorieLedger.Application.History;
using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Nutrition;

public sealed class DailyEnergyIntakeHistoryProvider:IDailyEnergyIntakeHistoryProvider {
    private readonly DailyJournalDaySnapshotProvider snapshotProvider;

    public DailyEnergyIntakeHistoryProvider(
        IFoodDiaryStore foodDiaryStore,
        IActivityStore? activityStore = null
    ) : this(
        new DailyJournalDaySnapshotProvider(
            new FoodDiaryDaySnapshotProvider(foodDiaryStore),
            activityStore ?? new InMemoryActivityStore())
    ) { }

    public DailyEnergyIntakeHistoryProvider(DailyJournalDaySnapshotProvider snapshotProvider) {
        ArgumentNullException.ThrowIfNull(snapshotProvider);
        this.snapshotProvider = snapshotProvider;
    }

    public IReadOnlyList<DailyEnergyIntakeEntry> GetEntries(DateOnly startDate, DateOnly endDate) {
        return [
            .. snapshotProvider.GetRange(startDate, endDate)
            .Select(day => new DailyEnergyIntakeEntry(
                Date: day.Date,
                CaloriesKcal: day.FoodDiary.ConsumedTotals.CaloriesKcal ?? 0m,
                IsComplete: day.FoodDiary.IsEnergyComplete,
                ExtraActivityBurnedCaloriesKcal: day.ExtraActivityBurnedCaloriesKcal)
            )
        ];
    }
}
