namespace CalorieLedger.Domain.Adaptive;

public sealed record AdaptiveEnergyRecommendationResult(
    AdaptiveEnergyRecommendationStatus Status,
    AdaptiveEnergyAdjustmentResult CurrentAdjustment,
    AdaptiveEnergyEvaluationEntry? CurrentEvaluationEntry,
    int ConsecutiveDeviationCount,
    int RequiredConsecutiveDeviationCount,
    int? DaysSincePreviousEvaluation) {
    public bool HasRecommendation =>
        Status
            == AdaptiveEnergyRecommendationStatus
                .RecommendationAvailable;

    public bool ShouldRecordEvaluation =>
        CurrentEvaluationEntry is not null;
}