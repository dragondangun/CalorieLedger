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

        var orderedMeasurements = new List<BodyMeasurementEntry>(allMeasurements);

        orderedMeasurements.Sort(
            static (left, right) => {
                var dateComparison =
                    left.Date.CompareTo(
                        right.Date
                    );

                if(dateComparison != 0) {
                    return dateComparison;
                }

                return left.Id.CompareTo(
                    right.Id
                );
            }
        );

        AllMeasurements = orderedMeasurements.AsReadOnly();

        var effectiveMeasurements = new List<BodyMeasurementEntry>();

        var hasFutureMeasurements = false;

        foreach(var measurement in orderedMeasurements) {
            if(measurement.Date > asOfDate) {
                hasFutureMeasurements = true;
                break;
            }

            effectiveMeasurements.Add(
                measurement
            );
        }

        EffectiveMeasurements = effectiveMeasurements.AsReadOnly();

        HasFutureMeasurements = hasFutureMeasurements;
    }
}
