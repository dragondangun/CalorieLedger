namespace CalorieLedger.Domain.Adaptive;

public enum AdaptiveEnergyAdjustmentStatus {
    InsufficientData = 1,
    EstimateUnavailable = 2,
    WithinTolerance = 3,
    CurrentTargetAlreadySuitable = 4,
    RecommendationAvailable = 5
}