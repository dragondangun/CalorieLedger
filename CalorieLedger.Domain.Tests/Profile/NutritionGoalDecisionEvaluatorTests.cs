using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Profile;

public sealed class NutritionGoalDecisionEvaluatorTests {
    [Fact]
    public void Evaluate_WeightLossGoalReached_ReturnsCompletionActions() {
        var body = CreateBodyProfile(
            weightKg: 74.8m,
            bodyFatPercent: 14.8m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m,
            EnergyBalancePercent: -15m);

        var result =
            NutritionGoalDecisionEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalDecisionStatus.GoalReached,
            result.Status);

        Assert.True(result.RequiresUserDecision);

        Assert.Contains(
            GoalNextAction.SwitchToMaintenance,
            result.AvailableActions);

        Assert.Contains(
            GoalNextAction.StartWeightGain,
            result.AvailableActions);

        Assert.Contains(
            GoalNextAction.SetNewGoal,
            result.AvailableActions);

        Assert.Contains(
            GoalNextAction.ContinueCurrentGoal,
            result.AvailableActions);
    }

    [Fact]
    public void Evaluate_PartiallyReachedWeightLossGoal_ReturnsPartialStatus() {
        var body = CreateBodyProfile(
            weightKg: 75m,
            bodyFatPercent: 18m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m);

        var result =
            NutritionGoalDecisionEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalDecisionStatus.PartiallyReached,
            result.Status);

        Assert.Contains(
            GoalNextAction.ContinueCurrentGoal,
            result.AvailableActions);

        Assert.Contains(
            GoalNextAction.SwitchToMaintenance,
            result.AvailableActions);
    }

    [Fact]
    public void Evaluate_MassGainStopLimitReached_TakesPriorityOverProgress() {
        var body = CreateBodyProfile(
            weightKg: 84m,
            bodyFatPercent: 18.5m,
            muscleMassKg: 39m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            TargetMuscleMassKg: 42m,
            EnergyBalancePercent: 5m,
            StopAtBodyFatPercent: 18m,
            MassGainIntent: MassGainIntent.LeanMassPriority);

        var result =
            NutritionGoalDecisionEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalDecisionStatus.StopLimitReached,
            result.Status);

        Assert.False(result.Progress.IsReached);
        Assert.True(result.StopCondition.ShouldStop);

        Assert.Contains(
            GoalNextAction.SwitchToMaintenance,
            result.AvailableActions);

        Assert.Contains(
            GoalNextAction.StartWeightLoss,
            result.AvailableActions);
    }

    [Fact]
    public void Evaluate_MissingBodyFatMeasurement_RequestsNewMeasurement() {
        var body = CreateBodyProfile(
            weightKg: 75m,
            bodyFatPercent: null);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m);

        var result =
            NutritionGoalDecisionEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalDecisionStatus.MeasurementMissing,
            result.Status);

        Assert.True(result.RequiresAttention);

        Assert.Contains(
            GoalNextAction.RepeatMeasurements,
            result.AvailableActions);
    }

    [Fact]
    public void Evaluate_InProgressGoal_AllowsContinuing() {
        var body = CreateBodyProfile(
            weightKg: 82m,
            bodyFatPercent: 20m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m);

        var result =
            NutritionGoalDecisionEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalDecisionStatus.InProgress,
            result.Status);

        Assert.False(result.RequiresUserDecision);

        Assert.Contains(
            GoalNextAction.ContinueCurrentGoal,
            result.AvailableActions);
    }

    [Fact]
    public void Evaluate_WithoutTargets_ReturnsNotConfigured() {
        var body = CreateBodyProfile(
            weightKg: 80m,
            bodyFatPercent: 18m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.Maintain);

        var result =
            NutritionGoalDecisionEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalDecisionStatus.NotConfigured,
            result.Status);

        Assert.Single(result.AvailableActions);

        Assert.Contains(
            GoalNextAction.SetNewGoal,
            result.AvailableActions);
    }

    private static BodyProfile CreateBodyProfile(
        decimal weightKg,
        decimal? bodyFatPercent,
        decimal? muscleMassKg = null,
        decimal? musclePercent = null) {
        return new BodyProfile(
            Sex: BiologicalSex.Male,
            AgeYears: 30,
            HeightCm: 180m,
            WeightKg: weightKg,
            BodyFatPercent: bodyFatPercent,
            BoneMassKg: null,
            MuscleMassKg: muscleMassKg,
            MusclePercent: musclePercent);
    }
}