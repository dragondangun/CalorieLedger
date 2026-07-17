namespace CalorieLedger.Domain.Adaptive;

public enum AdaptiveEnergyRecommendationStatus {
    NoRecommendation = 1,
    EvaluationTooSoon = 2,
    AwaitingConsistentDeviation = 3,
    RecommendationAvailable = 4
}