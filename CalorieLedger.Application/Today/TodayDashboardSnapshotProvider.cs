using CalorieLedger.Application.History;
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
    private readonly ICurrentDateProvider currentDateProvider;
    private readonly DailyJournalDaySnapshotProvider dailyJournalSnapshotProvider;

    public TodayDashboardSnapshotProvider(
        IUserNutritionProfileProvider profileProvider,
        DailyJournalDaySnapshotProvider dailyJournalSnapshotProvider,
        ICurrentDateProvider currentDateProvider
    ) {
        ArgumentNullException.ThrowIfNull(profileProvider);
        ArgumentNullException.ThrowIfNull(dailyJournalSnapshotProvider);
        ArgumentNullException.ThrowIfNull(currentDateProvider);

        this.profileProvider = profileProvider;
        this.dailyJournalSnapshotProvider = dailyJournalSnapshotProvider;
        this.currentDateProvider = currentDateProvider;
    }

    public TodayDashboardSnapshot GetToday() {
        var currentDate = currentDateProvider.GetCurrentDate();

        var weekStartDate = currentDate.AddDays(-(WeeklyDayCount - 1));

        var profile = profileProvider.GetCurrentProfile();

        var target = NutritionTargetCalculator.Calculate(profile);

        var goalDecision = NutritionGoalDecisionEvaluator.Evaluate(profile.Body, profile.Goal);

        var journalDays = dailyJournalSnapshotProvider.GetRange(weekStartDate, currentDate);
        var todayJournal = journalDays[^1];
        var todayDiary = todayJournal.FoodDiary;

        var weeklySummary = new WeeklyNutritionSummarySnapshot(
            [
                .. journalDays.Select(day => new DailyNutritionSummarySnapshot(
                    Date: day.Date,
                    ConsumedTotals: day.FoodDiary.ConsumedTotals,
                    IsEnergyComplete: day.FoodDiary.IsEnergyComplete,
                    AreMacrosComplete: day.FoodDiary.AreMacrosComplete,
                    ExtraActivityBurnedCaloriesKcal: day.ExtraActivityBurnedCaloriesKcal))
            ]
        );

        IReadOnlyList<TodayActivitySnapshotItem> activities = [
            .. todayJournal.Activities.Select(activity => new TodayActivitySnapshotItem(
                Id: activity.Id,
                Name: activity.Name,
                BurnedCaloriesKcal: activity.BurnedCaloriesKcal,
                StartedAt: activity.StartedAt,
                Duration: activity.Duration,
                Note: activity.Note))
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
