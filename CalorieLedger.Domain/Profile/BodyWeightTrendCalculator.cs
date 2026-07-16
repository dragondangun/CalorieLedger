namespace CalorieLedger.Domain.Profile;

public static class BodyWeightTrendCalculator {
    public const int DefaultWindowDays = 14;

    public const int DefaultMinimumMeasurementDays = 8;

    public static BodyWeightTrendResult Calculate(
        IEnumerable<BodyMeasurementEntry> measurements,
        DateOnly asOfDate,
        int windowDays = DefaultWindowDays,
        int minimumMeasurementDays = DefaultMinimumMeasurementDays) {
        ArgumentNullException.ThrowIfNull(measurements);

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

        var windowStartDate = asOfDate.AddDays(-(windowDays - 1));

        var measurementsInWindow = measurements.Where(
            measurement => measurement.Date >= windowStartDate
                && measurement.Date <= asOfDate).ToArray();

        if(measurementsInWindow.Any(measurement => measurement.WeightKg <= 0m)) {
            throw new ArgumentException(
                "Measurement weight must be greater than zero.",
                nameof(measurements));
        }

        var dailyMeasurements = measurementsInWindow
            .GroupBy(measurement => measurement.Date)
            .Select(group => new DailyWeight(
                Date: group.Key,
                WeightKg: group.Average(
                    measurement =>
                        measurement.WeightKg)))
            .OrderBy(measurement => measurement.Date)
            .ToArray();

        decimal? averageWeight = dailyMeasurements.Length == 0
                ? null : dailyMeasurements.Average(measurement => measurement.WeightKg);

        if(dailyMeasurements.Length
            < minimumMeasurementDays) {
            return new BodyWeightTrendResult(
                Status: BodyWeightTrendStatus.InsufficientData,
                WindowStartDate: windowStartDate,
                WindowEndDate: asOfDate,
                MeasurementDayCount: dailyMeasurements.Length,
                AverageWeightKg: averageWeight,
                EstimatedWeeklyChangeKg: null);
        }

        var originDayNumber = dailyMeasurements[0].Date.DayNumber;

        var meanDay = dailyMeasurements.Average(
            measurement => (decimal)(measurement.Date.DayNumber - originDayNumber));

        var meanWeight = dailyMeasurements.Average(measurement => measurement.WeightKg);

        var covarianceSum = dailyMeasurements.Sum(
            measurement =>
            {
                var day = (decimal)(measurement.Date.DayNumber - originDayNumber);

                return (day - meanDay) * (measurement.WeightKg - meanWeight);
            });

        var dayVarianceSum =dailyMeasurements.Sum(
            measurement =>
            {
                var day = (decimal)(measurement.Date.DayNumber - originDayNumber);

                var deviation = day - meanDay;

                return deviation * deviation;
            });

        if(dayVarianceSum == 0m) {
            return new BodyWeightTrendResult(
                Status: BodyWeightTrendStatus.InsufficientData,
                WindowStartDate: windowStartDate,
                WindowEndDate: asOfDate,
                MeasurementDayCount: dailyMeasurements.Length,
                AverageWeightKg: averageWeight,
                EstimatedWeeklyChangeKg: null);
        }

        var dailyChangeKg = covarianceSum / dayVarianceSum;

        var weeklyChangeKg = dailyChangeKg * 7m;

        return new BodyWeightTrendResult(
            Status: BodyWeightTrendStatus.Available,
            WindowStartDate: windowStartDate,
            WindowEndDate: asOfDate,
            MeasurementDayCount: dailyMeasurements.Length,
            AverageWeightKg: averageWeight,
            EstimatedWeeklyChangeKg: weeklyChangeKg);
    }

    private sealed record DailyWeight(
        DateOnly Date,
        decimal WeightKg);
}