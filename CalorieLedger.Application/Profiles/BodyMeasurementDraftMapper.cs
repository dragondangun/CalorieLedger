using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public static class BodyMeasurementDraftMapper {
    public static BodyMeasurementDraft FromEntry(
        BodyMeasurementEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        return new BodyMeasurementDraft(
            Id: entry.Id,
            Date: entry.Date,
            WeightKg: entry.WeightKg,
            BodyFatPercent: entry.BodyFatPercent,
            BoneMassKg: entry.BoneMassKg,
            MuscleMassKg: entry.MuscleMassKg,
            MusclePercent: entry.MusclePercent);
    }

    public static BodyMeasurementEntry ToEntry(
        BodyMeasurementDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        return new BodyMeasurementEntry(
            Id: draft.Id,
            Date: draft.Date,
            WeightKg: draft.WeightKg ?? 0m,
            BodyFatPercent: draft.BodyFatPercent,
            BoneMassKg: draft.BoneMassKg,
            MuscleMassKg: draft.MuscleMassKg,
            MusclePercent: draft.MusclePercent);
    }
}