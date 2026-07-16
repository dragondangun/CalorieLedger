using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Adaptive;

public sealed record AdaptivePlanDataQualityResult(
    DateOnly WindowStartDate,
    DateOnly WindowEndDate,
    int ObservationDaySpan,
    int WeightMeasurementDayCount,
    int CompleteIntakeDayCount,
    decimal? AverageDailyCaloriesKcal,
    BodyWeightTrendResult WeightTrend,
    AdaptivePlanDataIssue Issues) {
    public bool IsSufficient =>
        Issues == AdaptivePlanDataIssue.None;

    public bool HasIssue(
        AdaptivePlanDataIssue issue) {
        return (Issues & issue) != 0;
    }
}