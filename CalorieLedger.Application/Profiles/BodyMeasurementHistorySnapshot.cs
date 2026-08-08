using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class BodyMeasurementHistorySnapshot {
    public DateOnly AsOfDate { get; }
    public IReadOnlyList<BodyMeasurementEntry> EffectiveMeasurements { get; }
    public bool HasFutureMeasurements { get; }

    public BodyMeasurementEntry? LatestEffectiveMeasurement =>
        EffectiveMeasurements.Count == 0
            ? null
            : EffectiveMeasurements[^1];

    public BodyMeasurementHistorySnapshot(
        DateOnly asOfDate,
        IReadOnlyList<BodyMeasurementEntry> effectiveMeasurements,
        bool hasFutureMeasurements
    ) {
        ArgumentNullException.ThrowIfNull(effectiveMeasurements);

        AsOfDate = asOfDate;
        EffectiveMeasurements = effectiveMeasurements;
        HasFutureMeasurements = hasFutureMeasurements;
    }
}
