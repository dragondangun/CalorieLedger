using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class BodyTrendsViewModelFactoryTests {
    [Fact]
    public void Create_InsufficientData_ReturnsUnavailableTrends() {
        var asOfDate = new DateOnly(2026, 7, 28);

        var measurements = new[] {
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: asOfDate,
                WeightKg: 80m,
                BodyFatPercent: 25m)
        };

        var result = BodyTrendsViewModelFactory.Create(measurements, asOfDate);

        Assert.False(result.WeightTrend.IsAvailable);

        Assert.False(result.BodyFatTrend.IsAvailable);

        Assert.False(result.HasAnyAvailableTrend);
    }

    [Fact]
    public void Create_SufficientData_ReturnsAvailableTrends() {
        var asOfDate = new DateOnly(2026, 7, 28);

        var measurements = CreateDecreasingMeasurements(asOfDate);

        var result = BodyTrendsViewModelFactory.Create(measurements, asOfDate);

        Assert.True(result.WeightTrend.IsAvailable);

        Assert.True(result.BodyFatTrend.IsAvailable);

        Assert.True(result.HasAnyAvailableTrend);

        Assert.Equal(BodyTrendDirection.Decreasing, result.WeightTrend.Direction);

        Assert.Equal(BodyTrendDirection.Decreasing, result.BodyFatTrend.Direction);

        Assert.Contains("кг/нед.", result.WeightTrend.RateSummary);

        Assert.Contains("п.п./нед.", result.BodyFatTrend.RateSummary);

        Assert.Contains("охват:", result.BodyFatTrend.PeriodSummary);
    }

    [Fact]
    public void Create_UsesRussianDecimalSeparator() {
        var asOfDate = new DateOnly(2026, 7, 28);

        var result = BodyTrendsViewModelFactory.Create(
            CreateDecreasingMeasurements(asOfDate),
            asOfDate);

        Assert.Contains(
            ",",
            result.WeightTrend.CurrentValueSummary);

        Assert.Contains(
            ",",
            result.BodyFatTrend.CurrentValueSummary);
    }

    private static IReadOnlyList<BodyMeasurementEntry> CreateDecreasingMeasurements(DateOnly asOfDate) {
        var measurements = new List<BodyMeasurementEntry>();

        for(var dayOffset = 21; dayOffset >= 0; dayOffset--) {
            var elapsedDays = 21 - dayOffset;

            decimal? bodyFatPercent = dayOffset % 3 == 0
                ? 25m - elapsedDays * 0.02m
                : null;

            measurements.Add(new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: asOfDate.AddDays(-dayOffset),
                WeightKg: 80m - elapsedDays * 0.05m,
                BodyFatPercent: bodyFatPercent));
        }

        return measurements;
    }
}