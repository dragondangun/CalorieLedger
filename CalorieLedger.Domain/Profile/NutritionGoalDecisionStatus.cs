namespace CalorieLedger.Domain.Profile;

public enum NutritionGoalDecisionStatus {
    NotConfigured = 1,
    MeasurementMissing = 2,
    InProgress = 3,
    PartiallyReached = 4,
    GoalReached = 5,
    StopLimitReached = 6
}