using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed record BodyMeasurementHistorySnapshot(
    IReadOnlyList<BodyMeasurementEntry> EffectiveMeasurements,
    BodyMeasurementEntry? LatestEffectiveMeasurement,
    bool HasFutureMeasurements
);
