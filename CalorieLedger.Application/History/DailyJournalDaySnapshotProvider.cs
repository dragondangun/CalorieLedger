using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Meals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.History;

public sealed class DailyJournalDaySnapshotProvider {
    private readonly FoodDiaryDaySnapshotProvider foodDiarySnapshotProvider;
    private readonly IActivityStore activityStore;

    public DailyJournalDaySnapshotProvider(
        FoodDiaryDaySnapshotProvider foodDiarySnapshotProvider,
        IActivityStore activityStore
    ) {
        ArgumentNullException.ThrowIfNull(foodDiarySnapshotProvider);
        ArgumentNullException.ThrowIfNull(activityStore);

        this.foodDiarySnapshotProvider = foodDiarySnapshotProvider;
        this.activityStore = activityStore;
    }

    public DailyJournalDaySnapshot GetDay(DateOnly date) {
        return GetRange(date, date)[0];
    }

    public IReadOnlyList<DailyJournalDaySnapshot> GetRange(DateOnly startDate, DateOnly endDate) {
        var foodDays = foodDiarySnapshotProvider.GetRange(startDate, endDate);
        var activitiesByDate = activityStore.Get(startDate, endDate).ToLookup(x => x.Date);

        return [
            .. foodDays.Select(foodDay => new DailyJournalDaySnapshot(
                Date: foodDay.Date,
                FoodDiary: foodDay,
                Activities: [.. activitiesByDate[foodDay.Date]])
            )
        ];
    }
}
