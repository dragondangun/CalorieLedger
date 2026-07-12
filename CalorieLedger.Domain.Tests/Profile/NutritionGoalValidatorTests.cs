using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Profile;

public sealed class NutritionGoalValidatorTests {
    [Fact]
    public void Validate_WeightLossWithDeficit_ReturnsValid() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m,
            Strategy : EnergyStrategy.FromBalancePercent(15m));

        var result =
            NutritionGoalValidator.Validate(goal);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WeightGainWithBodyFatStopLimit_ReturnsValid() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            Strategy : EnergyStrategy.FromBalancePercent(5m),
            StopAtBodyFatPercent: 18m,
            MassGainIntent:
                MassGainIntent.LeanMassPriority);

        var result =
            NutritionGoalValidator.Validate(goal);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WeightLossWithZeroStrategy_ReturnsError() {
        var goal = new NutritionGoal(
        GoalType: WeightGoalType.LoseWeight,
        TargetWeightKg: 75m,
        Strategy:
            EnergyStrategy.FromBalancePercent(0m));

        var result =
        NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError
                .InvalidEnergyStrategyValue,
            result.Errors);
    }

    [Fact]
    public void Validate_WeightLossWithPositiveStrategy_ReturnsValid() {
        var goal = new NutritionGoal(
        GoalType: WeightGoalType.LoseWeight,
        TargetWeightKg: 75m,
        Strategy:
            EnergyStrategy.FromBalancePercent(5m));

        var result =
        NutritionGoalValidator.Validate(goal);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NegativeEnergyStrategyValue_ReturnsError() {
        var goal = new NutritionGoal(
        GoalType: WeightGoalType.LoseWeight,
        TargetWeightKg: 75m,
        Strategy: new EnergyStrategy(
            EnergyStrategyMode.BalancePercent,
            -15m));

        var result =
        NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError
                .InvalidEnergyStrategyValue,
            result.Errors);
    }

    [Fact]
    public void Validate_MaintenanceWithNonZeroStrategy_ReturnsError() {
        var goal = new NutritionGoal(
        GoalType: WeightGoalType.Maintain,
        Strategy: EnergyStrategy.FromWeightChangePerWeek(
            0.25m));

        var result = NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError.MaintenanceRequiresNeutralEnergyBalance,
            result.Errors);
    }

    [Fact]
    public void Validate_StopLimitOnWeightLoss_ReturnsError() {
        var goal = new NutritionGoal(
        GoalType: WeightGoalType.LoseWeight,
        TargetBodyFatPercent: 15m,
        Strategy:
            EnergyStrategy.FromBalancePercent(15m),
        StopAtBodyFatPercent: 18m);

        var result =
        NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError
                .StopBodyFatOnlyForWeightGain,
            result.Errors);
    }

    [Fact]
    public void Validate_InvalidPercentages_ReturnsErrors() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            TargetBodyFatPercent: 100m,
            TargetMusclePercent: 0m,
            Strategy: EnergyStrategy.FromBalancePercent(5m),
            StopAtBodyFatPercent: -1m);

        var result = NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError.InvalidTargetBodyFatPercent,
            result.Errors);

        Assert.Contains(
            NutritionGoalValidationError.InvalidTargetMusclePercent,
            result.Errors);

        Assert.Contains(
            NutritionGoalValidationError.InvalidStopBodyFatPercent,
            result.Errors);
    }

    [Fact]
    public void Validate_MissingEnergyStrategy_ReturnsError() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy: null);

        var result = NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError.MissingEnergyStrategy,
            result.Errors);
    }

    [Fact]
    public void Validate_MaintenanceWithZeroStrategy_ReturnsValid() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.Maintain,
            Strategy: EnergyStrategy.FromBalancePercent(0m));

        var result = NutritionGoalValidator.Validate(goal);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_OneHundredPercentStrategy_ReturnsError() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy: EnergyStrategy.FromBalancePercent(100m));

        var result = NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError.InvalidEnergyStrategyValue,
            result.Errors);
    }

    [Fact]
    public void Validate_UnknownStrategyMode_ReturnsError() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy: new EnergyStrategy(
                (EnergyStrategyMode)999,
                15m));

        var result = NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError.InvalidEnergyStrategyValue,
            result.Errors);
    }
}