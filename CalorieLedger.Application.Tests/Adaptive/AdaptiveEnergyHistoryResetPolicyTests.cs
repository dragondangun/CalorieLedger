using CalorieLedger.Application.Adaptive;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Tests.Application.Adaptive;

public sealed class
    AdaptiveEnergyHistoryResetPolicyTests {
    [Fact]
    public void ShouldReset_GoalTypeChanged_ReturnsTrue() {
        var previousGoal =
            new NutritionGoal(
                GoalType:
                    WeightGoalType.LoseWeight,
                Strategy:
                    EnergyStrategy
                        .FromBalancePercent(
                            15m));

        var updatedGoal =
            new NutritionGoal(
                GoalType:
                    WeightGoalType.GainWeight,
                Strategy:
                    EnergyStrategy
                        .FromBalancePercent(
                            15m));

        var result =
            AdaptiveEnergyHistoryResetPolicy
                .ShouldReset(
                    previousGoal,
                    updatedGoal);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReset_StrategyModeChanged_ReturnsTrue() {
        var previousGoal =
            new NutritionGoal(
                GoalType:
                    WeightGoalType.LoseWeight,
                Strategy:
                    EnergyStrategy
                        .FromBalancePercent(
                            15m));

        var updatedGoal =
            new NutritionGoal(
                GoalType:
                    WeightGoalType.LoseWeight,
                Strategy:
                    EnergyStrategy
                        .FromWeightChangePerWeek(
                            0.5m));

        var result =
            AdaptiveEnergyHistoryResetPolicy
                .ShouldReset(
                    previousGoal,
                    updatedGoal);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReset_StrategyValueChanged_ReturnsTrue() {
        var previousGoal =
            new NutritionGoal(
                GoalType:
                    WeightGoalType.LoseWeight,
                Strategy:
                    EnergyStrategy
                        .FromBalancePercent(
                            15m));

        var updatedGoal =
            new NutritionGoal(
                GoalType:
                    WeightGoalType.LoseWeight,
                Strategy:
                    EnergyStrategy
                        .FromBalancePercent(
                            20m));

        var result =
            AdaptiveEnergyHistoryResetPolicy
                .ShouldReset(
                    previousGoal,
                    updatedGoal);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReset_OnlyTargetValuesChanged_ReturnsFalse() {
        var previousGoal =
            new NutritionGoal(
                GoalType:
                    WeightGoalType.LoseWeight,
                TargetWeightKg:
                    75m,
                TargetBodyFatPercent:
                    15m,
                Strategy:
                    EnergyStrategy
                        .FromBalancePercent(
                            15m));

        var updatedGoal =
            new NutritionGoal(
                GoalType:
                    WeightGoalType.LoseWeight,
                TargetWeightKg:
                    72m,
                TargetBodyFatPercent:
                    13m,
                Strategy:
                    EnergyStrategy
                        .FromBalancePercent(
                            15m));

        var result =
            AdaptiveEnergyHistoryResetPolicy
                .ShouldReset(
                    previousGoal,
                    updatedGoal);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReset_IdenticalEnergyConfiguration_ReturnsFalse() {
        var previousGoal =
            new NutritionGoal(
                GoalType:
                    WeightGoalType.GainWeight,
                TargetWeightKg:
                    85m,
                StopAtBodyFatPercent:
                    18m,
                MassGainIntent:
                    MassGainIntent
                        .LeanMassPriority,
                Strategy:
                    EnergyStrategy
                        .FromBalancePercent(
                            5m));

        var updatedGoal =
            previousGoal with
            {
                TargetWeightKg = 87m,
                StopAtBodyFatPercent = 19m
            };

        var result =
            AdaptiveEnergyHistoryResetPolicy
                .ShouldReset(
                    previousGoal,
                    updatedGoal);

        Assert.False(result);
    }
}