namespace CalorieLedger.Domain.Profile;

public static class BodyFatTrendCalculator {
    public const int DefaultWindowDays = 28;

    public const int DefaultMinimumMeasurementDays = 8;

    public const int DefaultMinimumCoveredDays = 21;

    public static BodyFatTrendResult Calculate(
        IEnumerable<BodyMeasurementEntry> measurements,
        DateOnly asOfDate,
        int windowDays = DefaultWindowDays,
        int minimumMeasurementDays =
            DefaultMinimumMeasurementDays,
        int minimumCoveredDays =
            DefaultMinimumCoveredDays) {
        ArgumentNullException.ThrowIfNull(
            measurements);

        ValidateSettings(
            windowDays,
            minimumMeasurementDays,
            minimumCoveredDays);

        var windowStartDate =
            asOfDate.AddDays(
                -(windowDays - 1));

        var measurementsInWindow =
            measurements
                .Where(
                    measurement =>
                        measurement.Date
                            >= windowStartDate
                        && measurement.Date
                            <= asOfDate)
                .ToArray();

        if(measurementsInWindow.Any(
                measurement =>
                    measurement.BodyFatPercent
                        is <= 0m or >= 100m)) {
            throw new ArgumentException(
                "Body fat percentage must be greater than zero and less than one hundred.",
                nameof(measurements));
        }

        var dailyMeasurements =
            measurementsInWindow
                .Where(
                    measurement =>
                        measurement.BodyFatPercent
                            is not null)
                .GroupBy(
                    measurement =>
                        measurement.Date)
                .Select(
                    group =>
                        new DailyBodyFat(
                            Date: group.Key,
                            BodyFatPercent:
                                group.Average(
                                    measurement =>
                                        measurement
                                            .BodyFatPercent!
                                            .Value)))
                .OrderBy(
                    measurement =>
                        measurement.Date)
                .ToArray();

        decimal? averageBodyFatPercent =
            dailyMeasurements.Length == 0
                ? null
                : dailyMeasurements.Average(
                    measurement =>
                        measurement.BodyFatPercent);

        var coveredDaySpan =
            CalculateCoveredDaySpan(
                dailyMeasurements);

        if(dailyMeasurements.Length
                < minimumMeasurementDays
            || coveredDaySpan
                < minimumCoveredDays) {
            return new BodyFatTrendResult(
                Status:
                    BodyFatTrendStatus
                        .InsufficientData,
                WindowStartDate:
                    windowStartDate,
                WindowEndDate:
                    asOfDate,
                MeasurementDayCount:
                    dailyMeasurements.Length,
                CoveredDaySpan:
                    coveredDaySpan,
                AverageBodyFatPercent:
                    averageBodyFatPercent,
                EstimatedWeeklyChangePercentagePoints:
                    null);
        }

        var originDayNumber =
            dailyMeasurements[0]
                .Date
                .DayNumber;

        var meanDay =
            dailyMeasurements.Average(
                measurement =>
                    (decimal)(
                        measurement.Date.DayNumber
                        - originDayNumber));

        var meanBodyFat =
            dailyMeasurements.Average(
                measurement =>
                    measurement.BodyFatPercent);

        var covarianceSum =
            dailyMeasurements.Sum(
                measurement =>
                {
                    var day =
                        (decimal)(
                            measurement.Date.DayNumber
                            - originDayNumber);

                    return (day - meanDay)
                        * (measurement.BodyFatPercent
                            - meanBodyFat);
                });

        var dayVarianceSum =
            dailyMeasurements.Sum(
                measurement =>
                {
                    var day =
                        (decimal)(
                            measurement.Date.DayNumber
                            - originDayNumber);

                    var deviation =
                        day - meanDay;

                    return deviation * deviation;
                });

        if(dayVarianceSum == 0m) {
            return new BodyFatTrendResult(
                Status:
                    BodyFatTrendStatus
                        .InsufficientData,
                WindowStartDate:
                    windowStartDate,
                WindowEndDate:
                    asOfDate,
                MeasurementDayCount:
                    dailyMeasurements.Length,
                CoveredDaySpan:
                    coveredDaySpan,
                AverageBodyFatPercent:
                    averageBodyFatPercent,
                EstimatedWeeklyChangePercentagePoints:
                    null);
        }

        var dailyChangePercentagePoints =
            covarianceSum
            / dayVarianceSum;

        var weeklyChangePercentagePoints =
            dailyChangePercentagePoints
            * 7m;

        return new BodyFatTrendResult(
            Status:
                BodyFatTrendStatus.Available,
            WindowStartDate:
                windowStartDate,
            WindowEndDate:
                asOfDate,
            MeasurementDayCount:
                dailyMeasurements.Length,
            CoveredDaySpan:
                coveredDaySpan,
            AverageBodyFatPercent:
                averageBodyFatPercent,
            EstimatedWeeklyChangePercentagePoints:
                weeklyChangePercentagePoints);
    }

    private static int CalculateCoveredDaySpan(
        IReadOnlyList<DailyBodyFat>
            dailyMeasurements) {
        if(dailyMeasurements.Count == 0) {
            return 0;
        }

        return dailyMeasurements[^1]
                   .Date
                   .DayNumber
            - dailyMeasurements[0]
                .Date
                .DayNumber
            + 1;
    }

    private static void ValidateSettings(
        int windowDays,
        int minimumMeasurementDays,
        int minimumCoveredDays) {
        if(windowDays < 2) {
            throw new ArgumentOutOfRangeException(
                nameof(windowDays),
                windowDays,
                "Trend window must contain at least two days.");
        }

        if(minimumMeasurementDays < 2) {
            throw new ArgumentOutOfRangeException(
                nameof(minimumMeasurementDays),
                minimumMeasurementDays,
                "At least two measurement days are required.");
        }

        if(minimumMeasurementDays > windowDays) {
            throw new ArgumentException(
                "Minimum measurement days cannot exceed the trend window.",
                nameof(minimumMeasurementDays));
        }

        if(minimumCoveredDays < 2) {
            throw new ArgumentOutOfRangeException(
                nameof(minimumCoveredDays),
                minimumCoveredDays,
                "Minimum covered period must contain at least two days.");
        }

        if(minimumCoveredDays > windowDays) {
            throw new ArgumentException(
                "Minimum covered period cannot exceed the trend window.",
                nameof(minimumCoveredDays));
        }
    }

    private sealed record DailyBodyFat(
        DateOnly Date,
        decimal BodyFatPercent);
}