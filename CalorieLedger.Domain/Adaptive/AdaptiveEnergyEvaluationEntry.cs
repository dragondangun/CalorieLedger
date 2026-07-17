namespace CalorieLedger.Domain.Adaptive;

public sealed record AdaptiveEnergyEvaluationEntry(
    DateOnly EvaluationDate,
    AdaptiveEnergyAdjustmentStatus AdjustmentStatus,
    AdaptiveEnergyDeviationDirection DeviationDirection,
    decimal? ObservedWeeklyWeightChangeKg,
    decimal? TargetWeeklyWeightChangeKg,
    decimal? RecommendedDailyAdjustmentKcal,
    decimal? RecommendedTargetCaloriesKcal) {
    public static AdaptiveEnergyEvaluationEntry FromResult(
        DateOnly evaluationDate,
        AdaptiveEnergyAdjustmentResult result) {
        ArgumentNullException.ThrowIfNull(result);

        var direction =
            DetermineDirection(result);

        return new AdaptiveEnergyEvaluationEntry(
            EvaluationDate:
                evaluationDate,
            AdjustmentStatus:
                result.Status,
            DeviationDirection:
                direction,
            ObservedWeeklyWeightChangeKg:
                result.ObservedWeeklyWeightChangeKg,
            TargetWeeklyWeightChangeKg:
                result.TargetWeeklyWeightChangeKg,
            RecommendedDailyAdjustmentKcal:
                result.RecommendedDailyAdjustmentKcal,
            RecommendedTargetCaloriesKcal:
                result.RecommendedTargetCaloriesKcal);
    }

    private static AdaptiveEnergyDeviationDirection
        DetermineDirection(
            AdaptiveEnergyAdjustmentResult result) {
        if(result.Status
            != AdaptiveEnergyAdjustmentStatus
                .RecommendationAvailable) {
            return AdaptiveEnergyDeviationDirection.None;
        }

        var adjustment =
            result.RecommendedDailyAdjustmentKcal;

        if(adjustment is null
            || adjustment == 0m) {
            throw new ArgumentException(
                "An available recommendation must have a non-zero calorie adjustment.",
                nameof(result));
        }

        return adjustment > 0m
            ? AdaptiveEnergyDeviationDirection
                .IncreaseCalories
            : AdaptiveEnergyDeviationDirection
                .DecreaseCalories;
    }
}