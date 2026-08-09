using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class BodyMeasurementHistorySnapshot {
    public DateOnly AsOfDate { get; }
    public IReadOnlyList<BodyMeasurementEntry> AllMeasurements { get; }
    public IReadOnlyList<BodyMeasurementEntry> EffectiveMeasurements { get; }
    public bool HasFutureMeasurements { get; }

    public BodyMeasurementEntry? LatestEffectiveMeasurement =>
        EffectiveMeasurements.Count == 0
            ? null
            : EffectiveMeasurements[^1];

    public BodyMeasurementHistorySnapshot(
        DateOnly asOfDate,
        IReadOnlyList<BodyMeasurementEntry> allMeasurements
    ) {
        ArgumentNullException.ThrowIfNull(allMeasurements);

        AsOfDate = asOfDate;
        AllMeasurements = allMeasurements.ToArray();

        var effectiveMeasurements = new List<BodyMeasurementEntry>();

        var hasFutureMeasurements = false;

        foreach(var measurement in AllMeasurements) {
            if(measurement.Date <= asOfDate) {
                effectiveMeasurements.Add(measurement);
            }
            else {
                hasFutureMeasurements = true;
            }
        }

        EffectiveMeasurements = effectiveMeasurements.ToArray();

        HasFutureMeasurements = hasFutureMeasurements;
    }
}
