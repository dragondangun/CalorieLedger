using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Profile;

public sealed class BodyWeightTrendCalculatorTests {
    [Fact]
    public void Calculate_WithEightMeasurementDays_ReturnsRegressionTrend() {
        var startDate =
            new DateOnly(2026, 7, 1);

        var measurements = new[] {
            CreateMeasurement(
                startDate.AddDays(0),
                80.0m),

            CreateMeasurement(
                startDate.AddDays(1),
                79.9m),

            CreateMeasurement(
                startDate.AddDays(3),
                79.7m),

            CreateMeasurement(
                startDate.AddDays(5),
                79.5m),

            CreateMeasurement(
                startDate.AddDays(7),
                79.3m),

            CreateMeasurement(
                startDate.AddDays(9),
                79.1m),

            CreateMeasurement(
                startDate.AddDays(11),
                78.9m),

            CreateMeasurement(
                startDate.AddDays(13),
                78.7m)
        };

        var result = BodyWeightTrendCalculator.Calculate(
            measurements,
            asOfDate: startDate.AddDays(13));

        Assert.True(result.IsAvailable);

        Assert.Equal(
            BodyWeightTrendStatus.Available,
            result.Status);

        Assert.Equal(
            8,
            result.MeasurementDayCount);

        Assert.NotNull(result.EstimatedWeeklyChangeKg);

        Assert.Equal(
            -0.7m,
            Math.Round(
                result.EstimatedWeeklyChangeKg.Value,
                2));
    }

    [Fact]
    public void Calculate_WithTooFewMeasurementDays_ReturnsInsufficientData() {
        var startDate = new DateOnly(2026, 7, 1);

        var measurements = Enumerable.Range(0, 7)
            .Select(day =>
                CreateMeasurement(
                    startDate.AddDays(day),
                    80m - day * 0.1m))
            .ToArray();

        var result = BodyWeightTrendCalculator.Calculate(
            measurements,
            asOfDate: startDate.AddDays(13));

        Assert.False(result.IsAvailable);

        Assert.Equal(
            BodyWeightTrendStatus.InsufficientData,
            result.Status);

        Assert.Equal(
            7,
            result.MeasurementDayCount);

        Assert.NotNull(result.AverageWeightKg);

        Assert.Null(result.EstimatedWeeklyChangeKg);
    }

    [Fact]
    public void Calculate_WithMultipleMeasurementsOnSameDay_UsesDailyAverage() {
        var startDate = new DateOnly(2026, 7, 1);

        var measurements = new[] {
            CreateMeasurement(
                startDate,
                79.8m),

            CreateMeasurement(
                startDate,
                80.2m),

            CreateMeasurement(
                startDate.AddDays(1),
                79.9m),

            CreateMeasurement(
                startDate.AddDays(2),
                79.8m),

            CreateMeasurement(
                startDate.AddDays(3),
                79.7m),

            CreateMeasurement(
                startDate.AddDays(4),
                79.6m),

            CreateMeasurement(
                startDate.AddDays(5),
                79.5m),

            CreateMeasurement(
                startDate.AddDays(6),
                79.4m),

            CreateMeasurement(
                startDate.AddDays(7),
                79.3m)
        };

        var result = BodyWeightTrendCalculator.Calculate(
            measurements,
            asOfDate: startDate.AddDays(13));

        Assert.True(result.IsAvailable);

        Assert.Equal(
            8,
            result.MeasurementDayCount);

        Assert.Equal(
            -0.7m,
            Math.Round(
                result.EstimatedWeeklyChangeKg!.Value,
                2));
    }

    [Fact]
    public void Calculate_IgnoresMeasurementsOutsideWindow() {
        var asOfDate = new DateOnly(2026, 7, 14);

        var windowStartDate = asOfDate.AddDays(-13);

        var measurements = Enumerable
            .Range(0, 8)
            .Select(
                index =>
                    CreateMeasurement(
                        windowStartDate.AddDays(index),
                        80m - index * 0.1m))
            .Append(
                CreateMeasurement(
                    windowStartDate.AddDays(-1),
                    120m))
            .ToArray();

        var result = BodyWeightTrendCalculator.Calculate(
            measurements,
            asOfDate);

        Assert.True(result.IsAvailable);

        Assert.Equal(
            8,
            result.MeasurementDayCount);

        Assert.True(result.AverageWeightKg < 80m);
    }

    private static BodyMeasurementEntry CreateMeasurement(DateOnly date, decimal weightKg) {
        return new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: date,
            WeightKg: weightKg);
    }
}