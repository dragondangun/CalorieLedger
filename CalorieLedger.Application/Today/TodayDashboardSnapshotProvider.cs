using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Time;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;
using System;
using System.Linq;

namespace CalorieLedger.Application.Today;

public sealed class TodayDashboardSnapshotProvider:ITodayDashboardSnapshotProvider {
    private const int WeeklyDayCount = 7;

    private readonly IUserNutritionProfileProvider profileProvider;
    private readonly FoodDiaryDaySnapshotProvider foodDiaryDaySnapshotProvider;
    private readonly ICurrentDateProvider currentDateProvider;
    private readonly IActivityStore activityStore;

    public TodayDashboardSnapshotProvider(
        IUserNutritionProfileProvider profileProvider,
        FoodDiaryDaySnapshotProvider foodDiaryDaySnapshotProvider,
        IActivityStore activityStore,
        ICurrentDateProvider currentDateProvider
    ) {
        ArgumentNullException.ThrowIfNull(profileProvider);
        ArgumentNullException.ThrowIfNull(foodDiaryDaySnapshotProvider);
        ArgumentNullException.ThrowIfNull(activityStore);
        ArgumentNullException.ThrowIfNull(currentDateProvider);

        this.profileProvider = profileProvider;
        this.foodDiaryDaySnapshotProvider = foodDiaryDaySnapshotProvider;
        this.activityStore = activityStore;
        this.currentDateProvider = currentDateProvider;
    }

    public TodayDashboardSnapshot GetToday() {
        var currentDate = currentDateProvider.GetCurrentDate();

        var weekStartDate = currentDate.AddDays(-(WeeklyDayCount - 1));

        var profile = profileProvider.GetCurrentProfile();

        var target = NutritionTargetCalculator.Calculate(profile);

        var goalDecision = NutritionGoalDecisionEvaluator.Evaluate(profile.Body, profile.Goal);

        var diaryDays = foodDiaryDaySnapshotProvider.GetRange(
            weekStartDate,
            currentDate
        );

        var activityEntries = activityStore.Get(weekStartDate, currentDate);

        var activityByDate = activityEntries.GroupBy(entry => entry.Date).ToDictionary(
            group => group.Key,
            group => group.Sum(entry => entry.BurnedCaloriesKcal)
        );

        var todayDiary = diaryDays[^1];

        var weeklySummary = new WeeklyNutritionSummarySnapshot(
            [
                .. diaryDays.Select(
                    day => new DailyNutritionSummarySnapshot(
                        Date: day.Date,
                        ConsumedTotals: day.ConsumedTotals,
                        IsEnergyComplete: day.IsEnergyComplete,
                        AreMacrosComplete: day.AreMacrosComplete,
                        ExtraActivityBurnedCaloriesKcal: activityByDate.GetValueOrDefault(day.Date)
                    )
                ),
            ]
        );

        IReadOnlyList<TodayActivitySnapshotItem> activities = [
            .. activityEntries
                .Where(entry => entry.Date == currentDate)
                .Select(entry => new TodayActivitySnapshotItem(
                    Id: entry.Id,
                    Name: entry.Name,
                    BurnedCaloriesKcal: entry.BurnedCaloriesKcal,
                    StartedAt: entry.StartedAt,
                    Duration: entry.Duration,
                    Note: entry.Note))
        ];

        return new TodayDashboardSnapshot(
            Target: target,
            ConsumedTotals: todayDiary.ConsumedTotals,
            Meals: todayDiary.Meals,
            WeeklySummary: weeklySummary,
            Activities: activities,
            GoalDecision: goalDecision,
            IsFoodLogComplete: todayDiary.IsComplete
        );
    }
}
