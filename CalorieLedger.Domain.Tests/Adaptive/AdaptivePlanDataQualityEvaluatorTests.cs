using CalorieLedger.Domain.Adaptive;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Adaptive;

public sealed class AdaptivePlanDataQualityEvaluatorTests {
    [Fact]
    public void Evaluate_WithSufficientData_ReturnsAvailableResult() {
        var startDate =
            new DateOnly(2026, 7, 1);

        var asOfDate =
            startDate.AddDays(13);

        var bodyMeasurements =
            new[]
            {
                CreateBodyMeasurement(
                    startDate.AddDays(0),
                    80.0m),

                CreateBodyMeasurement(
                    startDate.AddDays(2),
                    79.8m),

                CreateBodyMeasurement(
                    startDate.AddDays(4),
                    79.6m),

                CreateBodyMeasurement(
                    startDate.AddDays(6),
                    79.4m),

                CreateBodyMeasurement(
                    startDate.AddDays(8),
                    79.2m),

                CreateBodyMeasurement(
                    startDate.AddDays(10),
                    79.0m),

                CreateBodyMeasurement(
                    startDate.AddDays(12),
                    78.8m),

                CreateBodyMeasurement(
                    startDate.AddDays(13),
                    78.7m)
            };

        var intakeEntries =
            Enumerable
                .Range(0, 10)
                .Select(
                    day =>
                        new DailyEnergyIntakeEntry(
                            Date:
                                startDate.AddDays(day),
                            CaloriesKcal:
                                2000m + day * 10m,
                            IsComplete:
                                true))
                .ToArray();

        var result =
            AdaptivePlanDataQualityEvaluator.Evaluate(
                bodyMeasurements,
                intakeEntries,
                asOfDate);

        Assert.True(result.IsSufficient);

        Assert.Equal(
            AdaptivePlanDataIssue.None,
            result.Issues);

        Assert.Equal(
            14,
            result.ObservationDaySpan);

        Assert.Equal(
            8,
            result.WeightMeasurementDayCount);

        Assert.Equal(
            10,
            result.CompleteIntakeDayCount);

        Assert.Equal(
            2045m,
            result.AverageDailyCaloriesKcal);

        Assert.True(
            result.WeightTrend.IsAvailable);
    }

    [Fact]
    public void Evaluate_WithTooFewCompleteIntakeDays_ReturnsIssue() {
        var startDate =
            new DateOnly(2026, 7, 1);

        var asOfDate =
            startDate.AddDays(13);

        var bodyMeasurements =
            Enumerable
                .Range(0, 8)
                .Select(
                    day =>
                        CreateBodyMeasurement(
                            startDate.AddDays(
                                day * 13 / 7),
                            80m - day * 0.1m))
                .ToArray();

        var intakeEntries =
            Enumerable
                .Range(0, 9)
                .Select(
                    day =>
                        new DailyEnergyIntakeEntry(
                            Date:
                                startDate.AddDays(day),
                            CaloriesKcal:
                                2000m,
                            IsComplete:
                                true))
                .ToArray();

        var result =
            AdaptivePlanDataQualityEvaluator.Evaluate(
                bodyMeasurements,
                intakeEntries,
                asOfDate);

        Assert.False(result.IsSufficient);

        Assert.True(
            result.HasIssue(
                AdaptivePlanDataIssue
                    .InsufficientCompleteIntakeDays));

        Assert.Equal(
            9,
            result.CompleteIntakeDayCount);
    }

    [Fact]
    public void Evaluate_IgnoresIncompleteIntakeDays() {
        var startDate =
            new DateOnly(2026, 7, 1);

        var asOfDate =
            startDate.AddDays(13);

        var bodyMeasurements =
            Enumerable
                .Range(0, 8)
                .Select(
                    day =>
                        CreateBodyMeasurement(
                            startDate.AddDays(
                                day * 13 / 7),
                            80m - day * 0.1m))
                .ToArray();

        var completeEntries =
            Enumerable
                .Range(0, 8)
                .Select(
                    day =>
                        new DailyEnergyIntakeEntry(
                            Date:
                                startDate.AddDays(day),
                            CaloriesKcal:
                                2000m,
                            IsComplete:
                                true));

        var incompleteEntries =
            Enumerable
                .Range(8, 4)
                .Select(
                    day =>
                        new DailyEnergyIntakeEntry(
                            Date:
                                startDate.AddDays(day),
                            CaloriesKcal:
                                1200m,
                            IsComplete:
                                false));

        var result =
            AdaptivePlanDataQualityEvaluator.Evaluate(
                bodyMeasurements,
                completeEntries.Concat(
                    incompleteEntries),
                asOfDate);

        Assert.Equal(
            8,
            result.CompleteIntakeDayCount);

        Assert.Equal(
            2000m,
            result.AverageDailyCaloriesKcal);

        Assert.True(
            result.HasIssue(
                AdaptivePlanDataIssue
                    .InsufficientCompleteIntakeDays));
    }

    [Fact]
    public void Evaluate_WithShortObservationPeriod_ReturnsIssue() {
        var startDate =
            new DateOnly(2026, 7, 6);

        var asOfDate =
            new DateOnly(2026, 7, 14);

        var bodyMeasurements =
            Enumerable
                .Range(0, 8)
                .Select(
                    day =>
                        CreateBodyMeasurement(
                            startDate.AddDays(day),
                            80m - day * 0.1m))
                .ToArray();

        var intakeEntries =
            Enumerable
                .Range(0, 9)
                .Select(
                    day =>
                        new DailyEnergyIntakeEntry(
                            Date:
                                startDate.AddDays(day),
                            CaloriesKcal:
                                2000m,
                            IsComplete:
                                true))
                .ToArray();

        var result =
            AdaptivePlanDataQualityEvaluator.Evaluate(
                bodyMeasurements,
                intakeEntries,
                asOfDate);

        Assert.False(result.IsSufficient);

        Assert.Equal(
            9,
            result.ObservationDaySpan);

        Assert.True(
            result.HasIssue(
                AdaptivePlanDataIssue
                    .ObservationPeriodTooShort));
    }

    [Fact]
    public void Evaluate_WithDuplicateIntakeDates_ThrowsArgumentException() {
        var date =
            new DateOnly(2026, 7, 1);

        var intakeEntries =
            new[]
            {
                new DailyEnergyIntakeEntry(
                    Date: date,
                    CaloriesKcal: 2000m,
                    IsComplete: true),

                new DailyEnergyIntakeEntry(
                    Date: date,
                    CaloriesKcal: 2100m,
                    IsComplete: true)
            };

        Assert.Throws<ArgumentException>(
            () =>
                AdaptivePlanDataQualityEvaluator.Evaluate(
                    bodyMeasurements:
                        Array.Empty<
                            BodyMeasurementEntry>(),
                    intakeEntries:
                        intakeEntries,
                    asOfDate:
                        date.AddDays(13)));
    }

    private static BodyMeasurementEntry
        CreateBodyMeasurement(
            DateOnly date,
            decimal weightKg) {
        return new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: date,
            WeightKg: weightKg);
    }
}