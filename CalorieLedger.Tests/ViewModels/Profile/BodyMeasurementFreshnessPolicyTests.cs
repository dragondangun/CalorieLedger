using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class BodyMeasurementFreshnessPolicyTests {
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(14, false)]
    [InlineData(15, true)]
    [InlineData(30, true)]
    public void IsStale_ReturnsExpectedValue(int ageInDays, bool expectedIsStale) {
        var currentDate = new DateOnly(2026, 8, 8);
        var measurementDate = currentDate.AddDays(-ageInDays);

        var isStale = BodyMeasurementFreshnessPolicy.IsStale(
            measurementDate,
            currentDate
        );

        Assert.Equal(
            expectedIsStale,
            isStale
        );
    }

    [Fact]
    public void GetAgeInDays_FutureMeasurement_ReturnsNegativeValue() {
        var currentDate = new DateOnly(2026, 8, 8);
        var measurementDate = new DateOnly(2026, 8, 10);

        var ageInDays = BodyMeasurementFreshnessPolicy.GetAgeInDays(
            measurementDate,
            currentDate
        );

        Assert.Equal(
            -2,
            ageInDays
        );
    }

    [Fact]
    public void IsStale_FutureMeasurement_ReturnsFalse() {
        var currentDate = new DateOnly(2026, 8, 8);
        var measurementDate = new DateOnly(2026, 8, 10);

        Assert.False(
            BodyMeasurementFreshnessPolicy.IsStale(
                measurementDate,
                currentDate
            )
        );
    }

    [Theory]
    [InlineData(-1, BodyMeasurementFreshnessState.Future)]
    [InlineData(0, BodyMeasurementFreshnessState.Fresh)]
    [InlineData(14, BodyMeasurementFreshnessState.Fresh)]
    [InlineData(15, BodyMeasurementFreshnessState.Stale)]
    public void GetState_ReturnsExpectedState(
        int ageInDays,
        BodyMeasurementFreshnessState expectedState
    ) {
        var currentDate = new DateOnly(2026, 8, 8);
        var measurementDate = currentDate.AddDays(-ageInDays);

        var state = BodyMeasurementFreshnessPolicy.GetState(
            measurementDate,
            currentDate
        );

        Assert.Equal(
            expectedState,
            state
        );
    }

    [Theory]
    [InlineData(-1, BodyMeasurementFreshnessState.Future)]
    [InlineData(0, BodyMeasurementFreshnessState.Fresh)]
    [InlineData(1, BodyMeasurementFreshnessState.Fresh)]
    [InlineData(14, BodyMeasurementFreshnessState.Fresh)]
    [InlineData(15, BodyMeasurementFreshnessState.Stale)]
    public void GetState_AgeInDays_ReturnsExpectedState(
    int ageInDays,
    BodyMeasurementFreshnessState expectedState) {
        var state =
        BodyMeasurementFreshnessPolicy.GetState(ageInDays);

        Assert.Equal(
            expectedState,
            state
        );
    }
}
