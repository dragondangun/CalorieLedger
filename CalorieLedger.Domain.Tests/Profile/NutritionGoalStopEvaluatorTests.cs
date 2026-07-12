using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Profile;

public sealed class NutritionGoalStopEvaluatorTests {
    [Fact]
    public void Evaluate_GainWeightBelowBodyFatLimit_ReturnsNotReached() {
        var body = CreateBodyProfile(
            bodyFatPercent: 17.5m);

        var goal = new NutritionGoal(
        GoalType: WeightGoalType.GainWeight,
        Strategy: EnergyStrategy.FromBalancePercent(5m),
        StopAtBodyFatPercent: 18m,
        MassGainIntent: MassGainIntent.LeanMassPriority);

        var result = NutritionGoalStopEvaluator.Evaluate(
            body,
            goal);

        Assert.Equal(
            GoalStopStatus.NotReached,
            result.Status);

        Assert.False(result.ShouldStop);

        Assert.Equal(
            17.5m,
            result.CurrentBodyFatPercent);

        Assert.Equal(
            18m,
            result.StopAtBodyFatPercent);
    }

    [Fact]
    public void Evaluate_GainWeightAtBodyFatLimit_ReturnsReached() {
        var body = CreateBodyProfile(
            bodyFatPercent: 18m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            Strategy: EnergyStrategy.FromBalancePercent(5m),
            StopAtBodyFatPercent: 18m,
            MassGainIntent: MassGainIntent.LeanMassPriority);

        var result = NutritionGoalStopEvaluator.Evaluate(
            body,
            goal);

        Assert.Equal(
            GoalStopStatus.Reached,
            result.Status);

        Assert.True(result.ShouldStop);
    }

    [Fact]
    public void Evaluate_GainWeightAboveBodyFatLimit_ReturnsReached() {
        var body = CreateBodyProfile(bodyFatPercent: 19.2m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            Strategy: EnergyStrategy.FromBalancePercent(5m),
            StopAtBodyFatPercent: 18m,
            MassGainIntent: MassGainIntent.TotalMass);

        var result = NutritionGoalStopEvaluator.Evaluate(body, goal);

        Assert.Equal(GoalStopStatus.Reached, result.Status);
        Assert.True(result.ShouldStop);
    }

    [Fact]
    public void Evaluate_WithoutCurrentBodyFat_ReturnsMeasurementMissing() {
        var body = CreateBodyProfile(bodyFatPercent: null);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            Strategy: EnergyStrategy.FromBalancePercent(5m),
            StopAtBodyFatPercent: 18m);

        var result = NutritionGoalStopEvaluator.Evaluate(body, goal);

        Assert.Equal(GoalStopStatus.MeasurementMissing, result.Status);
        Assert.False(result.ShouldStop);
    }

    [Fact]
    public void Evaluate_WithoutStopCondition_ReturnsNotConfigured() {
        var body = CreateBodyProfile(bodyFatPercent: 20m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            Strategy: EnergyStrategy.FromBalancePercent(5m));

        var result = NutritionGoalStopEvaluator.Evaluate(body, goal);

        Assert.Equal(GoalStopStatus.NotConfigured, result.Status);
        Assert.False(result.ShouldStop);
    }

    [Fact]
    public void Evaluate_StopConditionOnWeightLossGoal_ReturnsNotApplicable() {
        var body = CreateBodyProfile(bodyFatPercent: 20m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            Strategy: EnergyStrategy.FromBalancePercent(15m),
            StopAtBodyFatPercent: 18m);

        var result = NutritionGoalStopEvaluator.Evaluate(body, goal);

        Assert.Equal(GoalStopStatus.NotApplicable, result.Status);
        Assert.False(result.ShouldStop);
    }

    private static BodyProfile CreateBodyProfile(
        decimal? bodyFatPercent) {
        return new BodyProfile(
            Sex: BiologicalSex.Male,
            AgeYears: 30,
            HeightCm: 180m,
            WeightKg: 80m,
            BodyFatPercent: bodyFatPercent,
            BoneMassKg: null,
            MuscleMassKg: null,
            MusclePercent: null);
    }
}