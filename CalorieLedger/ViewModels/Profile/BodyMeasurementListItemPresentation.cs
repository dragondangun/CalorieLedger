namespace CalorieLedger.ViewModels.Profile;

public sealed record BodyMeasurementListItemPresentation(
    string DateSummary,
    string WeightSummary,
    string AdditionalValuesSummary,
    string ChangesSummary,
    string DataCompletenessText,
    bool IsLatest,
    string LatestBadgeText,
    bool IsLatestMeasurementStale,
    string MeasurementFreshnessWarning
);
