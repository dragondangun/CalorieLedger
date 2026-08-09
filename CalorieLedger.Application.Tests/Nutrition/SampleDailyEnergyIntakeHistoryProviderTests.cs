using CalorieLedger.Application.Nutrition;

namespace CalorieLedger.Application.Tests.Nutrition;

public sealed class SampleDailyEnergyIntakeHistoryProviderTests {
    [Fact]
    public void GetEntries_DateRange_ReturnsEveryDayInRange() {
        var provider = new SampleDailyEnergyIntakeHistoryProvider();

        var startDate = new DateOnly(2026, 7, 15);

        var endDate = new DateOnly(2026, 7, 28);

        var result = provider.GetEntries(
            startDate,
            endDate
        );

        Assert.Equal(
            14,
            result.Count
        );

        Assert.Equal(
            startDate,
            result[0].Date
        );

        Assert.Equal(
            endDate,
            result[^1].Date
        );

        Assert.All(
            result,
            entry => Assert.True(
                entry.IsComplete
            )
        );

        Assert.Equal(
            result.Count,
            result.Select(entry => entry.Date).Distinct().Count()
        );
    }

    [Fact]
    public void GetEntries_SingleDate_ReturnsSingleEntry() {
        var provider = new SampleDailyEnergyIntakeHistoryProvider();

        var date = new DateOnly(2026, 7, 28);

        var result = provider.GetEntries(date, date);

        Assert.Single(
            result
        );

        Assert.Equal(
            date,
            result[0].Date
        );
    }

    [Fact]
    public void GetEntries_EndBeforeStart_Throws() {
        var provider = new SampleDailyEnergyIntakeHistoryProvider();

        var startDate = new DateOnly(2026, 7, 28);

        var endDate = startDate.AddDays(-1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            provider.GetEntries(
                startDate,
                endDate
            )
        );
    }
}
