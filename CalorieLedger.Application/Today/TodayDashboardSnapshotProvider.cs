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

    public TodayDashboardSnapshotProvider(
        IUserNutritionProfileProvider profileProvider,
        FoodDiaryDaySnapshotProvider foodDiaryDaySnapshotProvider,
        ICurrentDateProvider currentDateProvider
    ) {
        ArgumentNullException.ThrowIfNull(
            profileProvider
        );

        ArgumentNullException.ThrowIfNull(
            foodDiaryDaySnapshotProvider
        );

        ArgumentNullException.ThrowIfNull(
            currentDateProvider
        );

        this.profileProvider =
            profileProvider;

        this.foodDiaryDaySnapshotProvider =
            foodDiaryDaySnapshotProvider;

        this.currentDateProvider =
            currentDateProvider;
    }

    public TodayDashboardSnapshot GetToday() {
        var currentDate =
            currentDateProvider.GetCurrentDate();

        var weekStartDate =
            currentDate.AddDays(
                -(WeeklyDayCount - 1)
            );

        var profile = profileProvider.GetCurrentProfile();

        var target = NutritionTargetCalculator.Calculate(profile);

        var goalDecision = NutritionGoalDecisionEvaluator.Evaluate(profile.Body, profile.Goal);

        var diaryDays = foodDiaryDaySnapshotProvider.GetRange(
            weekStartDate,
            currentDate
        );

        var todayDiary = diaryDays[^1];

        var weeklySummary = new WeeklyNutritionSummarySnapshot(
            [
                .. diaryDays.Select(
                    day => new DailyNutritionSummarySnapshot(
                        Date: day.Date,
                        ConsumedTotals: day.ConsumedTotals
                    )
                ),
            ]
        );

        return new TodayDashboardSnapshot(
            Target: target,
            ConsumedTotals: todayDiary.ConsumedTotals,
            Meals: todayDiary.Meals,
            WeeklySummary: weeklySummary,
            Activities: [],
            GoalDecision: goalDecision,
            IsFoodLogComplete: todayDiary.IsComplete
        );
    }
}
