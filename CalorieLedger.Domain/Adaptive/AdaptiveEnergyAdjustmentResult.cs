namespace CalorieLedger.Domain.Adaptive;

public sealed record AdaptiveEnergyAdjustmentResult(
    AdaptiveEnergyAdjustmentStatus Status,
    AdaptivePlanDataQualityResult DataQuality,
    decimal CurrentTargetCaloriesKcal,
    decimal? AverageDailyCaloriesKcal,
    decimal? EstimatedMaintenanceCaloriesKcal,
    decimal? ObservedWeeklyWeightChangeKg,
    decimal? TargetWeeklyWeightChangeKg,
    decimal? WeeklyChangeToleranceKg,
    decimal? PersonalizedTargetCaloriesKcal,
    decimal? RecommendedDailyAdjustmentKcal,
    decimal? RecommendedTargetCaloriesKcal) {
    public bool HasRecommendation => Status == AdaptiveEnergyAdjustmentStatus.RecommendationAvailable;
}