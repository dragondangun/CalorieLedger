using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Activities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.History;

public sealed record DailyJournalDaySnapshot(
    DateOnly Date,
    FoodDiaryDaySnapshot FoodDiary,
    IReadOnlyList<ActivityEntry> Activities
) {
    public decimal ExtraActivityBurnedCaloriesKcal => Activities.Sum(x => x.BurnedCaloriesKcal);

    public decimal ActivityAdjustedCaloriesKcal => (FoodDiary.ConsumedTotals.CaloriesKcal ?? 0m) - ExtraActivityBurnedCaloriesKcal;
}
