using CalorieLedger.Application.Profiles;
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

    public WeeklyJournalSummarySnapshot GetWeek(
        DateOnly selectedDate,
        DateOnly currentDate
    ) {
        if(selectedDate > currentDate) {
            throw new ArgumentOutOfRangeException(
                nameof(selectedDate),
                selectedDate,
                "Selected date cannot be in the future."
            );
        }

        var weekStart = GetWeekStart(selectedDate);
        var weekEnd = weekStart.AddDays(6);
        var availableEnd = weekEnd < currentDate ? weekEnd : currentDate;

        var days = journalSnapshotProvider.GetRange(weekStart, availableEnd);
        var completeDays = days.Where(day => day.FoodDiary.IsEnergyComplete).ToArray();

        decimal? averageFoodCalories = completeDays.Length == 0
            ? null : completeDays.Average(day => day.FoodDiary.ConsumedTotals.CaloriesKcal ?? 0m);

        decimal? averageExtraActivity = completeDays.Length == 0
            ? null : completeDays.Average(day => day.ExtraActivityBurnedCaloriesKcal);

        decimal? averageActivityAdjustedCalories = completeDays.Length == 0
            ? null : completeDays.Average(day => day.ActivityAdjustedCaloriesKcal);

        var measurements = bodyMeasurementHistoryService.GetAll()
            .Where(measurement =>
                measurement.Date >= weekStart
                && measurement.Date <= availableEnd
            )
            .OrderBy(measurement => measurement.Date)
            .ThenBy(measurement => measurement.Id)
            .ToArray();

        decimal? firstWeightKg = measurements.Length == 0
            ? null : measurements[0].WeightKg;

        decimal? lastWeightKg = measurements.Length == 0
            ? null : measurements[^1].WeightKg;

        decimal? weightChangeKg = measurements.Length < 2
            ? null : lastWeightKg!.Value - firstWeightKg!.Value;

        return new WeeklyJournalSummarySnapshot(
            WeekStartDate: weekStart,
            WeekEndDate: weekEnd,
            AvailableEndDate: availableEnd,
            AvailableDayCount: availableEnd.DayNumber - weekStart.DayNumber + 1,
            EnergyCompleteDayCount: completeDays.Length,
            MacroCompleteDayCount: days.Count(day => day.FoodDiary.AreMacrosComplete),
            AverageFoodCaloriesKcal: averageFoodCalories,
            AverageExtraActivityBurnedCaloriesKcal: averageExtraActivity,
            AverageActivityAdjustedCaloriesKcal: averageActivityAdjustedCalories,
            TotalExtraActivityBurnedCaloriesKcal: days.Sum(day => day.ExtraActivityBurnedCaloriesKcal),
            WeightMeasurementCount: measurements.Length,
            FirstWeightKg: firstWeightKg,
            LastWeightKg: lastWeightKg,
            WeightChangeKg: weightChangeKg
        );
    }

    private static DateOnly GetWeekStart(DateOnly date) {
        return date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
    }
}
