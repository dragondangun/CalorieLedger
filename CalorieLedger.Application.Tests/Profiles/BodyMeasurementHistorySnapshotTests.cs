using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class BodyMeasurementHistorySnapshotTests {
    [Fact]
    public void Constructor_UnsortedHistory_NormalizesChronologicalState() {
        var currentDate = new DateOnly(2026, 8, 8);

        var earlierMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(-2),
            WeightKg: 79m
        );

        var currentMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m
        );

        var futureMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(1),
            WeightKg: 81m
        );

        var snapshot = new BodyMeasurementHistorySnapshot(
            asOfDate: currentDate,
            allMeasurements: new[] {
                futureMeasurement,
                currentMeasurement,
                earlierMeasurement,
            }
        );

        Assert.Equal(
            3,
            snapshot.AllMeasurements.Count
        );

        Assert.Equal(
            earlierMeasurement,
            snapshot.AllMeasurements[0]
        );

        Assert.Equal(
            currentMeasurement,
            snapshot.AllMeasurements[1]
        );

        Assert.Equal(
            futureMeasurement,
            snapshot.AllMeasurements[2]
        );

        Assert.Equal(
            2,
            snapshot.EffectiveMeasurements.Count
        );

        Assert.Equal(
            earlierMeasurement,
            snapshot.EffectiveMeasurements[0]
        );

        Assert.Equal(
            currentMeasurement,
            snapshot.EffectiveMeasurements[1]
        );

        Assert.Equal(
            currentMeasurement,
            snapshot.LatestEffectiveMeasurement
        );

        Assert.True(
            snapshot.HasFutureMeasurements
        );
    }

    [Fact]
    public void Constructor_SourceListChangedAfterCreation_DoesNotChangeSnapshot() {
        var currentDate = new DateOnly(2026, 8, 8);

        var measurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m
        );

        var measurements = new List<BodyMeasurementEntry> {
            measurement,
        };

        var snapshot = new BodyMeasurementHistorySnapshot(
            asOfDate: currentDate,
            allMeasurements: measurements
        );

        measurements.Clear();

        Assert.Single(
            snapshot.AllMeasurements
        );

        Assert.Single(
            snapshot.EffectiveMeasurements
        );

        Assert.Equal(
            measurement,
            snapshot.LatestEffectiveMeasurement
        );
    }
}
