using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using System;
using System.Linq;

namespace CalorieLedger.Application.History;

public sealed class WeeklyJournalSummaryProvider {
    private readonly DailyJournalDaySnapshotProvider journalSnapshotProvider;
    private readonly BodyMeasurementHistoryService bodyMeasurementHistoryService;

    public WeeklyJournalSummaryProvider(
        DailyJournalDaySnapshotProvider journalSnapshotProvider,
        BodyMeasurementHistoryService bodyMeasurementHistoryService
    ) {
        ArgumentNullException.ThrowIfNull(journalSnapshotProvider);
        ArgumentNullException.ThrowIfNull(bodyMeasurementHistoryService);

        this.journalSnapshotProvider = journalSnapshotProvider;
        this.bodyMeasurementHistoryService = bodyMeasurementHistoryService;
    }

    public WeeklyJournalSummarySnapshot GetWeek(DateOnly selectedDate, DateOnly currentDate) {
        return GetRecentWeeks(selectedDate, currentDate, 1)[0];
    }

    public IReadOnlyList<WeeklyJournalSummarySnapshot> GetRecentWeeks(
        DateOnly selectedDate,
        DateOnly currentDate,
        int weekCount
    ) {
        if(selectedDate > currentDate) {
            throw new ArgumentOutOfRangeException(
                nameof(selectedDate),
                selectedDate,
                "Selected date cannot be in the future."
            );
        }

        if(weekCount <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(weekCount),
                weekCount,
                "Week count must be greater than zero."
            );
        }

        var lastWeekStart = GetWeekStart(selectedDate);
        var firstWeekStart = lastWeekStart.AddDays(-7 * (weekCount - 1));
        var rangeEnd = lastWeekStart.AddDays(6);
        if(rangeEnd > currentDate) {
            rangeEnd = currentDate;
        }

        var daysByWeek = journalSnapshotProvider.GetRange(firstWeekStart, rangeEnd).ToLookup(day => GetWeekStart(day.Date));

        var measurementsByWeek = bodyMeasurementHistoryService
            .GetAll()
            .Where(measurement => measurement.Date >= firstWeekStart && measurement.Date <= rangeEnd)
            .ToLookup(measurement => GetWeekStart(measurement.Date));

        var result = new List<WeeklyJournalSummarySnapshot>(weekCount);

        for(var offset = 0; offset < weekCount; offset++) {
            var weekStart = firstWeekStart.AddDays(offset * 7);

            result.Add(
                CreateWeekSummary(
                    weekStart,
                    currentDate,
                    daysByWeek[weekStart],
                    measurementsByWeek[weekStart]
                )
            );
        }

        return result;
    }

    private static WeeklyJournalSummarySnapshot CreateWeekSummary(
    DateOnly weekStart,
    DateOnly currentDate,
    IEnumerable<DailyJournalDaySnapshot> days,
    IEnumerable<BodyMeasurementEntry> measurements
) {
        var weekEnd = weekStart.AddDays(6);
        var availableEnd = weekEnd < currentDate ? weekEnd : currentDate;

        var availableDays = days.Where(day => day.Date <= availableEnd).ToArray();

        var completeDays = availableDays.Where(day => day.FoodDiary.IsEnergyComplete).ToArray();

        decimal? averageFoodCalories = completeDays.Length == 0 ? null : completeDays.Average(day => day.FoodDiary.ConsumedTotals.CaloriesKcal ?? 0m);

        decimal? averageExtraActivity = completeDays.Length == 0 ? null : completeDays.Average(day => day.ExtraActivityBurnedCaloriesKcal);

        decimal? averageAdjustedCalories = completeDays.Length == 0 ? null : completeDays.Average(day => day.ActivityAdjustedCaloriesKcal);

        var orderedMeasurements = measurements
            .Where(measurement => measurement.Date <= availableEnd)
            .OrderBy(measurement => measurement.Date)
            .ThenBy(measurement => measurement.Id)
            .ToArray();

        decimal? firstWeightKg = orderedMeasurements.Length == 0 ? null : orderedMeasurements[0].WeightKg;

        decimal? lastWeightKg = orderedMeasurements.Length == 0 ? null : orderedMeasurements[^1].WeightKg;

        decimal? weightChangeKg = orderedMeasurements.Length < 2 ? null : lastWeightKg!.Value - firstWeightKg!.Value;

        return new WeeklyJournalSummarySnapshot(
            WeekStartDate: weekStart,
            WeekEndDate: weekEnd,
            AvailableEndDate: availableEnd,
            AvailableDayCount: availableEnd.DayNumber - weekStart.DayNumber + 1,
            EnergyCompleteDayCount: completeDays.Length,
            MacroCompleteDayCount: availableDays.Count(day => day.FoodDiary.AreMacrosComplete),
            AverageFoodCaloriesKcal: averageFoodCalories,
            AverageExtraActivityBurnedCaloriesKcal: averageExtraActivity,
            AverageActivityAdjustedCaloriesKcal: averageAdjustedCalories,
            TotalExtraActivityBurnedCaloriesKcal: availableDays.Sum(day => day.ExtraActivityBurnedCaloriesKcal),
            WeightMeasurementCount: orderedMeasurements.Length,
            FirstWeightKg: firstWeightKg,
            LastWeightKg: lastWeightKg,
            WeightChangeKg: weightChangeKg
        );
    }

    private static DateOnly GetWeekStart(DateOnly date) {
        return date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
    }
}
