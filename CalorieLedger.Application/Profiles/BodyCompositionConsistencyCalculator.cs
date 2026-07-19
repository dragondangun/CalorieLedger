using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed record BodyCompositionConsistencyResult(
    decimal? FatMassKg,
    decimal? FatFreeMassKg,
    decimal? BonePercent,
    decimal? ResidualMassKg,
    bool IsConsistent);

public static class BodyCompositionConsistencyCalculator {
    public const decimal CompositionToleranceKg = 0.3m;

    private const int CalculatedValueDecimals = 2;

    public static BodyCompositionConsistencyResult Evaluate(
        BodyMeasurementEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        if(entry.WeightKg <= 0m) {
            return new BodyCompositionConsistencyResult(
                FatMassKg: null,
                FatFreeMassKg: null,
                BonePercent: null,
                ResidualMassKg: null,
                IsConsistent: true);
        }

        var fatMassKg =
            CalculateFatMass(
                entry.WeightKg,
                entry.BodyFatPercent);

        decimal? fatFreeMassKg = fatMassKg is null
            ? null
            : entry.WeightKg - fatMassKg.Value;

        var bonePercent =
            CalculateBonePercent(
                entry.WeightKg,
                entry.BoneMassKg);

        var residualMassKg =
            CalculateResidualMass(entry, fatMassKg);

        var isConsistent =
            IsComponentMassValid(
                entry.MuscleMassKg,
                entry.WeightKg)
            && IsComponentMassValid(
                entry.BoneMassKg,
                entry.WeightKg)
            && IsWithinFatFreeMass(
                entry.MuscleMassKg,
                entry.BoneMassKg,
                fatFreeMassKg)
            && residualMassKg
                is null
                    or >= -CompositionToleranceKg;

        return new BodyCompositionConsistencyResult(
            FatMassKg: fatMassKg,
            FatFreeMassKg: fatFreeMassKg,
            BonePercent: bonePercent,
            ResidualMassKg: residualMassKg,
            IsConsistent: isConsistent);
    }

    public static decimal? CalculateFatMass(
        decimal weightKg,
        decimal? bodyFatPercent) {
        if(weightKg <= 0m
            || bodyFatPercent is null
            || bodyFatPercent <= 0m
            || bodyFatPercent >= 100m) {
            return null;
        }

        return Math.Round(
            weightKg
            * bodyFatPercent.Value
            / 100m,
            CalculatedValueDecimals,
            MidpointRounding.AwayFromZero);
    }

    public static decimal? CalculateBonePercent(
        decimal weightKg,
        decimal? boneMassKg) {
        if(weightKg <= 0m
            || boneMassKg is null
            || boneMassKg <= 0m) {
            return null;
        }

        return Math.Round(
            boneMassKg.Value
            / weightKg
            * 100m,
            CalculatedValueDecimals,
            MidpointRounding.AwayFromZero);
    }

    private static decimal? CalculateResidualMass(
        BodyMeasurementEntry entry,
        decimal? fatMassKg) {
        var knownMassKg = 0m;
        var hasKnownComponent = false;

        if(fatMassKg is not null) {
            knownMassKg += fatMassKg.Value;
            hasKnownComponent = true;
        }

        if(entry.MuscleMassKg is > 0m) {
            knownMassKg += entry.MuscleMassKg.Value;
            hasKnownComponent = true;
        }

        if(entry.BoneMassKg is > 0m) {
            knownMassKg += entry.BoneMassKg.Value;
            hasKnownComponent = true;
        }

        if(!hasKnownComponent) {
            return null;
        }

        return Math.Round(
            entry.WeightKg - knownMassKg,
            CalculatedValueDecimals,
            MidpointRounding.AwayFromZero);
    }

    private static bool IsComponentMassValid(
        decimal? componentMassKg,
        decimal weightKg) {
        return componentMassKg is null
            || componentMassKg
                <= weightKg
                + CompositionToleranceKg;
    }

    private static bool IsWithinFatFreeMass(
        decimal? muscleMassKg,
        decimal? boneMassKg,
        decimal? fatFreeMassKg) {
        if(fatFreeMassKg is null) {
            return true;
        }

        if(muscleMassKg
            > fatFreeMassKg
            + CompositionToleranceKg) {
            return false;
        }

        if(boneMassKg
            > fatFreeMassKg
            + CompositionToleranceKg) {
            return false;
        }

        if(muscleMassKg is not null
            && boneMassKg is not null
            && muscleMassKg.Value
                + boneMassKg.Value
                > fatFreeMassKg.Value
                + CompositionToleranceKg) {
            return false;
        }

        return true;
    }
}