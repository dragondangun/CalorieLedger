namespace CalorieLedger.Domain.Profile;

public enum NutritionGoalProgressStatus {
    NotConfigured = 1,
    MeasurementMissing = 2,
    InProgress = 3,
    PartiallyReached = 4,
    Reached = 5
}