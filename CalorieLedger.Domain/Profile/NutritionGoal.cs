namespace CalorieLedger.Domain.Profile;

public sealed record NutritionGoal(
    WeightGoalType GoalType,
    decimal? TargetWeightKg = null,
    decimal? DesiredWeightChangeKgPerWeek = null);