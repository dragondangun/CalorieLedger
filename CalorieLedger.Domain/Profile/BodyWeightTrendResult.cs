namespace CalorieLedger.Domain.Profile;

public sealed record BodyWeightTrendResult(
    BodyWeightTrendStatus Status,
    DateOnly WindowStartDate,
    DateOnly WindowEndDate,
    int MeasurementDayCount,
    decimal? AverageWeightKg,
    decimal? EstimatedWeeklyChangeKg) {
    public bool IsAvailable => Status == BodyWeightTrendStatus.Available;
}