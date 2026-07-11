namespace CalorieLedger.Domain.Profile;

public sealed record NutritionGoalDecision(
    NutritionGoalDecisionStatus Status,
    NutritionGoalProgressEvaluation Progress,
    GoalStopEvaluation StopCondition,
    IReadOnlyList<GoalNextAction> AvailableActions) {
    public bool RequiresUserDecision =>
        Status is NutritionGoalDecisionStatus.PartiallyReached
            or NutritionGoalDecisionStatus.GoalReached
            or NutritionGoalDecisionStatus.StopLimitReached;

    public bool RequiresAttention =>
        RequiresUserDecision
        || Status == NutritionGoalDecisionStatus.MeasurementMissing;
}