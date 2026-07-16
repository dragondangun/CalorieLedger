namespace CalorieLedger.Domain.Adaptive;

[Flags]
public enum AdaptivePlanDataIssue {
    None = 0,

    ObservationPeriodTooShort = 1,
    InsufficientWeightMeasurementDays = 2,
    InsufficientCompleteIntakeDays = 4,
    WeightTrendUnavailable = 8
}