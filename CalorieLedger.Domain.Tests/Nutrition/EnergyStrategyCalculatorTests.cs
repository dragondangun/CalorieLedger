using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Nutrition;

public sealed class EnergyStrategyCalculatorTests {
    [Fact]
    public void Calculate_WeightLossByPercent_ReturnsDeficitAndPredictedLoss() {
        var strategy =
            EnergyStrategy.FromBalancePercent(15m);

        var result =
            EnergyStrategyCalculator.Calculate(
                strategy,
                WeightGoalType.LoseWeight,
                maintenanceCaloriesKcal: 2500m);

        Assert.Equal(
            -375m,
            result.DailyEnergyAdjustmentKcal);

        Assert.Equal(
            -15m,
            result.EnergyBalancePercent);

        Assert.Equal(
            2125m,
            result.TargetCaloriesKcal);

        Assert.Equal(
            -0.34m,
            Math.Round(
                result.PredictedWeightChangeKgPerWeek,
                2));
    }

    [Fact]
    public void Calculate_WeightLossByWeeklyChange_ReturnsRequiredDeficit() {
        var strategy =
            EnergyStrategy.FromWeightChangePerWeek(
                0.5m);

        var result =
            EnergyStrategyCalculator.Calculate(
                strategy,
                WeightGoalType.LoseWeight,
                maintenanceCaloriesKcal: 2500m);

        Assert.Equal(
            -550m,
            result.DailyEnergyAdjustmentKcal);

        Assert.Equal(
            -22m,
            result.EnergyBalancePercent);

        Assert.Equal(
            -0.5m,
            result.PredictedWeightChangeKgPerWeek);

        Assert.Equal(
            1950m,
            result.TargetCaloriesKcal);
    }

    [Fact]
    public void Calculate_WeightGainByPercent_ReturnsSurplusAndPredictedGain() {
        var strategy =
            EnergyStrategy.FromBalancePercent(5m);

        var result =
            EnergyStrategyCalculator.Calculate(
                strategy,
                WeightGoalType.GainWeight,
                maintenanceCaloriesKcal: 2500m);

        Assert.Equal(
            125m,
            result.DailyEnergyAdjustmentKcal);

        Assert.Equal(
            5m,
            result.EnergyBalancePercent);

        Assert.Equal(
            2625m,
            result.TargetCaloriesKcal);

        Assert.Equal(
            0.11m,
            Math.Round(
                result.PredictedWeightChangeKgPerWeek,
                2));
    }

    [Fact]
    public void Calculate_WeightGainByWeeklyChange_ReturnsRequiredSurplus() {
        var strategy =
            EnergyStrategy.FromWeightChangePerWeek(
                0.25m);

        var result =
            EnergyStrategyCalculator.Calculate(
                strategy,
                WeightGoalType.GainWeight,
                maintenanceCaloriesKcal: 2500m);

        Assert.Equal(
            275m,
            result.DailyEnergyAdjustmentKcal);

        Assert.Equal(
            11m,
            result.EnergyBalancePercent);

        Assert.Equal(
            0.25m,
            result.PredictedWeightChangeKgPerWeek);

        Assert.Equal(
            2775m,
            result.TargetCaloriesKcal);
    }

    [Fact]
    public void Calculate_MaintenanceWithZeroStrategy_ReturnsNeutralBalance() {
        var strategy =
            EnergyStrategy.FromBalancePercent(0m);

        var result =
            EnergyStrategyCalculator.Calculate(
                strategy,
                WeightGoalType.Maintain,
                maintenanceCaloriesKcal: 2500m);

        Assert.Equal(
            0m,
            result.DailyEnergyAdjustmentKcal);

        Assert.Equal(
            0m,
            result.EnergyBalancePercent);

        Assert.Equal(
            0m,
            result.PredictedWeightChangeKgPerWeek);

        Assert.Equal(
            2500m,
            result.TargetCaloriesKcal);
    }

    [Fact]
    public void Calculate_WithNegativeStrategyValue_ThrowsArgumentOutOfRangeException() {
        var strategy =
            EnergyStrategy.FromBalancePercent(-15m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => EnergyStrategyCalculator.Calculate(
                strategy,
                WeightGoalType.LoseWeight,
                maintenanceCaloriesKcal: 2500m));
    }

    [Fact]
    public void Calculate_MaintenanceWithNonZeroStrategy_ThrowsArgumentException() {
        var strategy =
            EnergyStrategy.FromBalancePercent(10m);

        Assert.Throws<ArgumentException>(
            () => EnergyStrategyCalculator.Calculate(
                strategy,
                WeightGoalType.Maintain,
                maintenanceCaloriesKcal: 2500m));
    }
}