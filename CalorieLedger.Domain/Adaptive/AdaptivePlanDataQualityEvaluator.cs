using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Adaptive;

public static class AdaptivePlanDataQualityEvaluator {
    public const int DefaultWindowDays = 14;

    public const int DefaultMinimumObservationDays = 14;

    public const int DefaultMinimumWeightMeasurementDays = 8;

    public const int DefaultMinimumCompleteIntakeDays = 10;

    public static AdaptivePlanDataQualityResult Evaluate(
        IEnumerable<BodyMeasurementEntry> bodyMeasurements,
        IEnumerable<DailyEnergyIntakeEntry> intakeEntries,
        DateOnly asOfDate,
        int windowDays = DefaultWindowDays,
        int minimumObservationDays =
            DefaultMinimumObservationDays,
        int minimumWeightMeasurementDays =
            DefaultMinimumWeightMeasurementDays,
        int minimumCompleteIntakeDays =
            DefaultMinimumCompleteIntakeDays) {
        ArgumentNullException.ThrowIfNull(
            bodyMeasurements);

        ArgumentNullException.ThrowIfNull(
            intakeEntries);

        ValidateSettings(
            windowDays,
            minimumObservationDays,
            minimumWeightMeasurementDays,
            minimumCompleteIntakeDays);

        var windowStartDate =
            asOfDate.AddDays(
                -(windowDays - 1));

        var measurementsInWindow =
            bodyMeasurements
                .Where(
                    entry =>
                        entry.Date >= windowStartDate
                        && entry.Date <= asOfDate)
                .ToArray();

        var intakeEntriesInWindow =
            intakeEntries
                .Where(
                    entry =>
                        entry.Date >= windowStartDate
                        && entry.Date <= asOfDate)
                .ToArray();

        ValidateIntakeEntries(
            intakeEntriesInWindow);

        var weightMeasurementDayCount =
            measurementsInWindow
                .Select(
                    entry => entry.Date)
                .Distinct()
                .Count();

        var completeIntakeEntries =
            intakeEntriesInWindow
                .Where(
                    entry => entry.IsComplete)
                .OrderBy(
                    entry => entry.Date)
                .ToArray();

        var completeIntakeDayCount =
            completeIntakeEntries.Length;

        decimal? averageDailyCaloriesKcal =
            completeIntakeEntries.Length == 0
                ? null
                : completeIntakeEntries.Average(
                    entry => entry.CaloriesKcal);

        var observationDaySpan =
            CalculateObservationDaySpan(
                measurementsInWindow,
                completeIntakeEntries,
                asOfDate);

        var weightTrend =
            BodyWeightTrendCalculator.Calculate(
                measurements:
                    measurementsInWindow,
                asOfDate:
                    asOfDate,
                windowDays:
                    windowDays,
                minimumMeasurementDays:
                    minimumWeightMeasurementDays);

        var issues =
            AdaptivePlanDataIssue.None;

        if(observationDaySpan
            < minimumObservationDays) {
            issues |=
                AdaptivePlanDataIssue
                    .ObservationPeriodTooShort;
        }

        if(weightMeasurementDayCount
            < minimumWeightMeasurementDays) {
            issues |=
                AdaptivePlanDataIssue
                    .InsufficientWeightMeasurementDays;
        }

        if(completeIntakeDayCount
            < minimumCompleteIntakeDays) {
            issues |=
                AdaptivePlanDataIssue
                    .InsufficientCompleteIntakeDays;
        }

        if(!weightTrend.IsAvailable
            && weightMeasurementDayCount
                >= minimumWeightMeasurementDays) {
            issues |=
                AdaptivePlanDataIssue
                    .WeightTrendUnavailable;
        }

        return new AdaptivePlanDataQualityResult(
            WindowStartDate:
                windowStartDate,
            WindowEndDate:
                asOfDate,
            ObservationDaySpan:
                observationDaySpan,
            WeightMeasurementDayCount:
                weightMeasurementDayCount,
            CompleteIntakeDayCount:
                completeIntakeDayCount,
            AverageDailyCaloriesKcal:
                averageDailyCaloriesKcal,
            WeightTrend:
                weightTrend,
            Issues:
                issues);
    }

    private static int CalculateObservationDaySpan(
        IReadOnlyCollection<BodyMeasurementEntry>
            bodyMeasurements,
        IReadOnlyCollection<DailyEnergyIntakeEntry>
            completeIntakeEntries,
        DateOnly asOfDate) {
        var firstBodyMeasurementDate =
            bodyMeasurements.Count == 0
                ? (DateOnly?)null
                : bodyMeasurements.Min(
                    entry => entry.Date);

        var firstCompleteIntakeDate =
            completeIntakeEntries.Count == 0
                ? (DateOnly?)null
                : completeIntakeEntries.Min(
                    entry => entry.Date);

        var firstObservationDate =
            GetEarlierDate(
                firstBodyMeasurementDate,
                firstCompleteIntakeDate);

        if(firstObservationDate is null) {
            return 0;
        }

        return asOfDate.DayNumber
            - firstObservationDate.Value.DayNumber
            + 1;
    }

    private static DateOnly? GetEarlierDate(
        DateOnly? first,
        DateOnly? second) {
        if(first is null) {
            return second;
        }

        if(second is null) {
            return first;
        }

        return first.Value <= second.Value
            ? first
            : second;
    }

    private static void ValidateIntakeEntries(
        IReadOnlyCollection<DailyEnergyIntakeEntry>
            intakeEntries) {
        if(intakeEntries.Any(
                entry =>
                    entry.CaloriesKcal < 0m)) {
            throw new ArgumentException(
                "Daily calorie intake cannot be negative.",
                nameof(intakeEntries));
        }

        var hasDuplicateDates =
            intakeEntries
                .GroupBy(
                    entry => entry.Date)
                .Any(
                    group => group.Count() > 1);

        if(hasDuplicateDates) {
            throw new ArgumentException(
                "Only one daily energy intake entry is allowed per date.",
                nameof(intakeEntries));
        }
    }

    private static void ValidateSettings(
        int windowDays,
        int minimumObservationDays,
        int minimumWeightMeasurementDays,
        int minimumCompleteIntakeDays) {
        if(windowDays < 2) {
            throw new ArgumentOutOfRangeException(
                nameof(windowDays),
                windowDays,
                "The data window must contain at least two days.");
        }

        if(minimumObservationDays < 2
            || minimumObservationDays > windowDays) {
            throw new ArgumentOutOfRangeException(
                nameof(minimumObservationDays),
                minimumObservationDays,
                "Minimum observation days must be between two and the window length.");
        }

        if(minimumWeightMeasurementDays < 2
            || minimumWeightMeasurementDays > windowDays) {
            throw new ArgumentOutOfRangeException(
                nameof(minimumWeightMeasurementDays),
                minimumWeightMeasurementDays,
                "Minimum weight measurement days must be between two and the window length.");
        }

        if(minimumCompleteIntakeDays < 1
            || minimumCompleteIntakeDays > windowDays) {
            throw new ArgumentOutOfRangeException(
                nameof(minimumCompleteIntakeDays),
                minimumCompleteIntakeDays,
                "Minimum complete intake days must be between one and the window length.");
        }
    }
}