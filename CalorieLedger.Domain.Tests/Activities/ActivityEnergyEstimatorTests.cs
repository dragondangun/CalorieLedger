using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Domain.Tests.Activities;

public sealed class ActivityEnergyEstimatorTests {
    [Fact]
    public void EstimateExtraCalories_SubtractsRestingMet() {
        var result = ActivityEnergyEstimator.EstimateExtraCalories(
            6m,
            60m,
            TimeSpan.FromHours(1)
        );

        Assert.Equal(300m, result);
    }

    [Fact]
    public void EstimateExtraCalories_HalfHour_UsesFractionalDuration() {
        var result = ActivityEnergyEstimator.EstimateExtraCalories(
            4m,
            60m,
            TimeSpan.FromMinutes(30)
        );

        Assert.Equal(90m, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.9)]
    public void EstimateExtraCalories_MetBelowOne_Throws(decimal metValue) {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ActivityEnergyEstimator.EstimateExtraCalories(
                metValue,
                60m,
                TimeSpan.FromHours(1)
            )
        );
    }
}
