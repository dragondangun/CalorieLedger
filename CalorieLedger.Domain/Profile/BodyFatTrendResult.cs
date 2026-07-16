namespace CalorieLedger.Domain.Profile;

public sealed record BodyFatTrendResult(
    BodyFatTrendStatus Status,
    DateOnly WindowStartDate,
    DateOnly WindowEndDate,
    int MeasurementDayCount,
    int CoveredDaySpan,
    decimal? AverageBodyFatPercent,
    decimal? EstimatedWeeklyChangePercentagePoints) {
    public bool IsAvailable =>
        Status == BodyFatTrendStatus.Available;
}