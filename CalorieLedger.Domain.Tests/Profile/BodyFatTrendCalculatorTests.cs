using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Profile;

public sealed class BodyFatTrendCalculatorTests {
    [Fact]
    public void Calculate_WithSufficientData_ReturnsBodyFatTrend() {
        var startDate =
            new DateOnly(2026, 7, 1);

        var measurementDays =
            new[]
            {
                0,
                4,
                8,
                12,
                16,
                20,
                24,
                27
            };

        var measurements =
            measurementDays
                .Select(
                    day =>
                        CreateMeasurement(
                            date:
                                startDate.AddDays(day),
                            bodyFatPercent:
                                16m
                                + day * 0.02m))
                .ToArray();

        var result =
            BodyFatTrendCalculator.Calculate(
                measurements,
                asOfDate:
                    startDate.AddDays(27));

        Assert.True(result.IsAvailable);

        Assert.Equal(
            BodyFatTrendStatus.Available,
            result.Status);

        Assert.Equal(
            8,
            result.MeasurementDayCount);

        Assert.Equal(
            28,
            result.CoveredDaySpan);

        Assert.NotNull(
            result.EstimatedWeeklyChangePercentagePoints);

        Assert.Equal(
            0.14m,
            Math.Round(
                result
                    .EstimatedWeeklyChangePercentagePoints
                    .Value,
                2));
    }

    [Fact]
    public void Calculate_WithTooFewMeasurements_ReturnsInsufficientData() {
        var startDate =
            new DateOnly(2026, 7, 1);

        var measurements =
            Enumerable
                .Range(0, 7)
                .Select(
                    index =>
                        CreateMeasurement(
                            startDate.AddDays(
                                index * 4),
                            16m + index * 0.1m))
                .ToArray();

        var result =
            BodyFatTrendCalculator.Calculate(
                measurements,
                asOfDate:
                    startDate.AddDays(27));

        Assert.False(result.IsAvailable);

        Assert.Equal(
            BodyFatTrendStatus.InsufficientData,
            result.Status);

        Assert.Equal(
            7,
            result.MeasurementDayCount);

        Assert.Null(
            result.EstimatedWeeklyChangePercentagePoints);
    }

    [Fact]
    public void Calculate_WithShortCoveredPeriod_ReturnsInsufficientData() {
        var startDate =
            new DateOnly(2026, 7, 1);

        var measurements =
            Enumerable
                .Range(0, 8)
                .Select(
                    day =>
                        CreateMeasurement(
                            startDate.AddDays(day),
                            16m + day * 0.05m))
                .ToArray();

        var result =
            BodyFatTrendCalculator.Calculate(
                measurements,
                asOfDate:
                    startDate.AddDays(27));

        Assert.False(result.IsAvailable);

        Assert.Equal(
            8,
            result.MeasurementDayCount);

        Assert.Equal(
            8,
            result.CoveredDaySpan);

        Assert.Null(
            result.EstimatedWeeklyChangePercentagePoints);
    }

    [Fact]
    public void Calculate_IgnoresMeasurementsWithoutBodyFat() {
        var startDate =
            new DateOnly(2026, 7, 1);

        var validMeasurements =
            new[]
            {
                0,
                4,
                8,
                12,
                16,
                20,
                24,
                27
            }
            .Select(
                day =>
                    CreateMeasurement(
                        startDate.AddDays(day),
                        16m + day * 0.02m));

        var weightOnlyMeasurements =
            Enumerable
                .Range(0, 10)
                .Select(
                    day =>
                        CreateMeasurement(
                            startDate.AddDays(day),
                            bodyFatPercent: null));

        var measurements =
            validMeasurements
                .Concat(
                    weightOnlyMeasurements)
                .ToArray();

        var result =
            BodyFatTrendCalculator.Calculate(
                measurements,
                asOfDate:
                    startDate.AddDays(27));

        Assert.True(result.IsAvailable);

        Assert.Equal(
            8,
            result.MeasurementDayCount);
    }

    [Fact]
    public void Calculate_WithMultipleMeasurementsOnSameDay_UsesDailyAverage() {
        var startDate = new DateOnly(2026, 7, 1);

        var measurements = new[] {
            CreateMeasurement(
                startDate,
                15.8m),

            CreateMeasurement(
                startDate,
                16.2m),

            CreateMeasurement(
                startDate.AddDays(4),
                16.08m),

            CreateMeasurement(
                startDate.AddDays(8),
                16.16m),

            CreateMeasurement(
                startDate.AddDays(12),
                16.24m),

            CreateMeasurement(
                startDate.AddDays(16),
                16.32m),

            CreateMeasurement(
                startDate.AddDays(20),
                16.40m),

            CreateMeasurement(
                startDate.AddDays(24),
                16.48m),

            CreateMeasurement(
                startDate.AddDays(27),
                16.54m)
        };

        var result =
            BodyFatTrendCalculator.Calculate(
                measurements,
                asOfDate:
                    startDate.AddDays(27));

        Assert.True(result.IsAvailable);

        Assert.Equal(
            8,
            result.MeasurementDayCount);

        Assert.Equal(
            0.14m,
            Math.Round(
                result
                    .EstimatedWeeklyChangePercentagePoints!
                    .Value,
                2));
    }

    [Fact]
    public void Calculate_WithInvalidBodyFatValue_ThrowsArgumentException() {
        var measurements = new[]
        {
            CreateMeasurement(
                new DateOnly(2026, 7, 1),
                100m)
        };

        Assert.Throws<ArgumentException>(
            () =>
                BodyFatTrendCalculator.Calculate(
                    measurements,
                    asOfDate:
                        new DateOnly(2026, 7, 28)));
    }

    private static BodyMeasurementEntry CreateMeasurement(
        DateOnly date,
        decimal? bodyFatPercent) {
        return new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: date,
            WeightKg: 80m,
            BodyFatPercent:
                bodyFatPercent);
    }
}