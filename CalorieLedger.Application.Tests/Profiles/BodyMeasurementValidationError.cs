using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class
    BodyMeasurementMuscleValueNormalizerTests {
    [Fact]
    public void Normalize_MuscleMassOnly_CalculatesPercent() {
        var entry =
            CreateEntry(
                weightKg: 80m,
                muscleMassKg: 35m,
                musclePercent: null);

        var result =
            BodyMeasurementMuscleValueNormalizer
                .Normalize(entry);

        Assert.Equal(
            35m,
            result.MuscleMassKg);

        Assert.Equal(
            43.75m,
            result.MusclePercent);
    }

    [Fact]
    public void Normalize_MusclePercentOnly_CalculatesMass() {
        var entry =
            CreateEntry(
                weightKg: 80m,
                muscleMassKg: null,
                musclePercent: 43.75m);

        var result =
            BodyMeasurementMuscleValueNormalizer
                .Normalize(entry);

        Assert.Equal(
            35m,
            result.MuscleMassKg);

        Assert.Equal(
            43.75m,
            result.MusclePercent);
    }

    [Fact]
    public void Normalize_BothValues_PreservesEnteredValues() {
        var entry =
            CreateEntry(
                weightKg: 80m,
                muscleMassKg: 35m,
                musclePercent: 43.8m);

        var result =
            BodyMeasurementMuscleValueNormalizer
                .Normalize(entry);

        Assert.Equal(
            entry,
            result);
    }

    [Theory]
    [InlineData(43.75, true)]
    [InlineData(43.8, true)]
    [InlineData(43.6, true)]
    [InlineData(43.5, false)]
    [InlineData(40.0, false)]
    public void AreValuesConsistent_ChecksTolerance(
        double musclePercent,
        bool expected) {
        var entry =
            CreateEntry(
                weightKg: 80m,
                muscleMassKg: 35m,
                musclePercent:
                    (decimal)musclePercent);

        var result =
            BodyMeasurementMuscleValueNormalizer
                .AreValuesConsistent(entry);

        Assert.Equal(
            expected,
            result);
    }

    private static BodyMeasurementEntry CreateEntry(
        decimal weightKg,
        decimal? muscleMassKg,
        decimal? musclePercent) {
        return new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date:
                new DateOnly(
                    2026,
                    7,
                    19),
            WeightKg: weightKg,
            MuscleMassKg: muscleMassKg,
            MusclePercent: musclePercent);
    }
}