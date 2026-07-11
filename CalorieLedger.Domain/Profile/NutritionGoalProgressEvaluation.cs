namespace CalorieLedger.Domain.Profile;

public sealed record NutritionGoalProgressEvaluation(
    NutritionGoalProgressStatus Status,
    GoalMetricStatus WeightStatus,
    GoalMetricStatus BodyFatStatus,
    GoalMetricStatus MuscleMassStatus,
    GoalMetricStatus MusclePercentStatus) {
    public bool IsReached =>
        Status == NutritionGoalProgressStatus.Reached;

    public bool HasReachedTargets =>
        Status is NutritionGoalProgressStatus.PartiallyReached
            or NutritionGoalProgressStatus.Reached;
}