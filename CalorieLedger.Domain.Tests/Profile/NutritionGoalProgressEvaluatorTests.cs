using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Profile;

public sealed class NutritionGoalProgressEvaluatorTests {
    [Fact]
    public void Evaluate_WeightLossTargetReached_ReturnsReached() {
        var body = CreateBodyProfile(
            weightKg: 74.8m,
            bodyFatPercent: null);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m);

        var result = NutritionGoalProgressEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalProgressStatus.Reached,
            result.Status);

        Assert.Equal(
            GoalMetricStatus.Reached,
            result.WeightStatus);

        Assert.True(result.IsReached);
    }

    [Fact]
    public void Evaluate_BodyFatLossTargetReached_ReturnsReached() {
        var body = CreateBodyProfile(
            weightKg: 80m,
            bodyFatPercent: 14.8m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetBodyFatPercent: 15m);

        var result = NutritionGoalProgressEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalProgressStatus.Reached,
            result.Status);

        Assert.Equal(
            GoalMetricStatus.Reached,
            result.BodyFatStatus);
    }

    [Fact]
    public void Evaluate_WeightReachedButBodyFatNotReached_ReturnsPartiallyReached() {
        var body = CreateBodyProfile(
            weightKg: 75m,
            bodyFatPercent: 18m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m);

        var result = NutritionGoalProgressEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalProgressStatus.PartiallyReached,
            result.Status);

        Assert.Equal(
            GoalMetricStatus.Reached,
            result.WeightStatus);

        Assert.Equal(
            GoalMetricStatus.NotReached,
            result.BodyFatStatus);

        Assert.True(result.HasReachedTargets);
        Assert.False(result.IsReached);
    }

    [Fact]
    public void Evaluate_WeightAndBodyFatReached_ReturnsReached() {
        var body = CreateBodyProfile(
            weightKg: 74.5m,
            bodyFatPercent: 14.5m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m);

        var result = NutritionGoalProgressEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalProgressStatus.Reached,
            result.Status);

        Assert.True(result.IsReached);
    }

    [Fact]
    public void Evaluate_MissingBodyFatMeasurement_ReturnsMeasurementMissing() {
        var body = CreateBodyProfile(
            weightKg: 75m,
            bodyFatPercent: null);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m);

        var result = NutritionGoalProgressEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalProgressStatus.MeasurementMissing,
            result.Status);

        Assert.Equal(
            GoalMetricStatus.Reached,
            result.WeightStatus);

        Assert.Equal(
            GoalMetricStatus.MeasurementMissing,
            result.BodyFatStatus);
    }

    [Fact]
    public void Evaluate_MuscleMassTargetReached_ReturnsReached() {
        var body = CreateBodyProfile(
            weightKg: 85m,
            bodyFatPercent: 17m,
            muscleMassKg: 40m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            TargetMuscleMassKg: 40m,
            MassGainIntent: MassGainIntent.LeanMassPriority);

        var result = NutritionGoalProgressEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalProgressStatus.Reached,
            result.Status);

        Assert.Equal(
            GoalMetricStatus.Reached,
            result.MuscleMassStatus);
    }

    [Fact]
    public void Evaluate_WithoutConfiguredTargets_ReturnsNotConfigured() {
        var body = CreateBodyProfile(
            weightKg: 80m,
            bodyFatPercent: 18m);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.Maintain);

        var result = NutritionGoalProgressEvaluator.Evaluate(body, goal);

        Assert.Equal(
            NutritionGoalProgressStatus.NotConfigured,
            result.Status);

        Assert.False(result.IsReached);
        Assert.False(result.HasReachedTargets);
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