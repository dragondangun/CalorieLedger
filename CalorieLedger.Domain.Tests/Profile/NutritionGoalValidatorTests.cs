using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Profile;

public sealed class NutritionGoalValidatorTests {
    [Fact]
    public void Validate_WeightLossWithDeficit_ReturnsValid() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m,
            EnergyBalancePercent: -15m);

        var result =
            NutritionGoalValidator.Validate(goal);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WeightGainWithBodyFatStopLimit_ReturnsValid() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            EnergyBalancePercent: 5m,
            StopAtBodyFatPercent: 18m,
            MassGainIntent:
                MassGainIntent.LeanMassPriority);

        var result =
            NutritionGoalValidator.Validate(goal);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WeightLossWithSurplus_ReturnsError() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            EnergyBalancePercent: 5m);

        var result =
            NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError
                .WeightLossRequiresDeficit,
            result.Errors);
    }

    [Fact]
    public void Validate_WeightGainWithDeficit_ReturnsError() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            TargetWeightKg: 85m,
            EnergyBalancePercent: -10m,
            MassGainIntent:
                MassGainIntent.TotalMass);

        var result =
            NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError
                .WeightGainRequiresSurplus,
            result.Errors);
    }

    [Fact]
    public void Validate_WithBothEnergyStrategies_ReturnsError() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            DesiredWeightChangeKgPerWeek: 0.5m,
            EnergyBalancePercent: -15m);

        var result =
            NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError
                .ConflictingEnergyStrategies,
            result.Errors);
    }

    [Fact]
    public void Validate_MaintenanceWithWeightChange_ReturnsError() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.Maintain,
            DesiredWeightChangeKgPerWeek: 0.25m);

        var result =
            NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError
                .WeightChangeNotAllowedForMaintenance,
            result.Errors);
    }

    [Fact]
    public void Validate_StopLimitOnWeightLoss_ReturnsError() {
        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetBodyFatPercent: 15m,
            EnergyBalancePercent: -15m,
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
            EnergyBalancePercent: 5m,
            StopAtBodyFatPercent: -1m);

        var result =
            NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError
                .InvalidTargetBodyFatPercent,
            result.Errors);

        Assert.Contains(
            NutritionGoalValidationError
                .InvalidTargetMusclePercent,
            result.Errors);

        Assert.Contains(
            NutritionGoalValidationError
                .InvalidStopBodyFatPercent,
            result.Errors);
    }

    [Fact]
    public void Validate_UnifiedWeightLossStrategy_ReturnsValid() {
        var goal = new NutritionGoal(
        GoalType: WeightGoalType.LoseWeight,
        TargetWeightKg: 75m,
        Strategy:
            EnergyStrategy.FromBalancePercent(15m));

        var result =
        NutritionGoalValidator.Validate(goal);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_UnifiedWeightGainStrategy_ReturnsValid() {
        var goal = new NutritionGoal(
        GoalType: WeightGoalType.GainWeight,
        TargetWeightKg: 85m,
        Strategy:
            EnergyStrategy.FromBalancePercent(5m),
        MassGainIntent:
            MassGainIntent.LeanMassPriority);

        var result =
        NutritionGoalValidator.Validate(goal);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_UnifiedAndLegacyStrategies_ReturnsConflict() {
        var goal = new NutritionGoal(
        GoalType: WeightGoalType.LoseWeight,
        TargetWeightKg: 75m,
        EnergyBalancePercent: -15m,
        Strategy:
            EnergyStrategy.FromBalancePercent(15m));

        var result =
        NutritionGoalValidator.Validate(goal);

        Assert.False(result.IsValid);

        Assert.Contains(
            NutritionGoalValidationError
                .ConflictingLegacyAndUnifiedEnergyStrategies,
            result.Errors);
    }

    [Fact]
    public void Validate_ZeroUnifiedWeightLossStrategy_ReturnsError() {
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
}