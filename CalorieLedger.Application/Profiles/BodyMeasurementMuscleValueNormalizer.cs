using CalorieLedger.Domain.Profile;
using System;

namespace CalorieLedger.Application.Profiles;

public static class
    BodyMeasurementMuscleValueNormalizer {
    public const decimal
        MusclePercentTolerance = 0.2m;

    private const int CalculatedValueDecimals = 2;

    public static BodyMeasurementEntry Normalize(
        BodyMeasurementEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        if(entry.WeightKg <= 0m) {
            return entry;
        }

        if(entry.MuscleMassKg is not null
            && entry.MusclePercent is null) {
            var calculatedPercent =
                CalculateMusclePercent(
                    weightKg: entry.WeightKg,
                    muscleMassKg:
                        entry.MuscleMassKg.Value);

            return entry with
            {
                MusclePercent =
                    calculatedPercent
            };
        }

        if(entry.MuscleMassKg is null
            && entry.MusclePercent is not null) {
            var calculatedMass =
                CalculateMuscleMass(
                    weightKg: entry.WeightKg,
                    musclePercent:
                        entry.MusclePercent.Value);

            return entry with
            {
                MuscleMassKg =
                    calculatedMass
            };
        }

        return entry;
    }

    public static bool AreValuesConsistent(BodyMeasurementEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        if(entry.MuscleMassKg is null
            || entry.MusclePercent is null) {
            return true;
        }

        if(entry.WeightKg <= 0m
            || entry.MuscleMassKg <= 0m
            || entry.MuscleMassKg
                > entry.WeightKg
            || entry.MusclePercent <= 0m
            || entry.MusclePercent >= 100m) {
            // Ошибки отдельных значений проверяет
            // BodyMeasurementHistoryService.
            return true;
        }

        var expectedPercent = entry.MuscleMassKg.Value
            / entry.WeightKg
            * 100m;

        var difference = Math.Abs(expectedPercent - entry.MusclePercent.Value);

        return difference <= MusclePercentTolerance;
    }

    public static decimal CalculateMusclePercent(
        decimal weightKg,
        decimal muscleMassKg) {
        if(weightKg <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(weightKg));
        }

        if(muscleMassKg <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(muscleMassKg));
        }

        return Math.Round(
            muscleMassKg / weightKg * 100m,
            CalculatedValueDecimals,
            MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateMuscleMass(
        decimal weightKg,
        decimal musclePercent) {
        if(weightKg <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(weightKg));
        }

        if(musclePercent <= 0m
            || musclePercent >= 100m) {
            throw new ArgumentOutOfRangeException(
                nameof(musclePercent));
        }

        return Math.Round(
            weightKg * musclePercent / 100m,
            CalculatedValueDecimals,
            MidpointRounding.AwayFromZero);
    }
}