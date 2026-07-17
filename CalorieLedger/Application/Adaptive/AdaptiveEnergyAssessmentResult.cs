using CalorieLedger.Domain.Adaptive;

namespace CalorieLedger.Application.Adaptive;

public sealed record AdaptiveEnergyAssessmentResult(
    AdaptivePlanDataQualityResult DataQuality,
    AdaptiveEnergyAdjustmentResult Adjustment,
    AdaptiveEnergyRecommendationResult Recommendation) {
    public bool HasRecommendation =>
        Recommendation.HasRecommendation;
}