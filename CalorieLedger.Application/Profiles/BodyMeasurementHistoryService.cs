using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class BodyMeasurementHistoryService {
    private readonly IBodyMeasurementStore
        _store;

    public BodyMeasurementHistoryService(
        IBodyMeasurementStore store) {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
    }

    public IReadOnlyList<BodyMeasurementEntry>
        GetAll() {
        return _store.GetAll();
    }

    public BodyMeasurementSaveResult Save(
        BodyMeasurementEntry entry,
        DateOnly currentDate) {
        ArgumentNullException.ThrowIfNull(entry);

        var errors = Validate(
            entry,
            currentDate);

        if(errors.Count > 0) {
            return new BodyMeasurementSaveResult(
                IsSuccess: false,
                Errors: errors);
        }

        var normalizedEntry = BodyMeasurementMuscleValueNormalizer.Normalize(entry);

        _store.Save(normalizedEntry);

        return new BodyMeasurementSaveResult(
            IsSuccess: true,
            Errors: Array.Empty<BodyMeasurementValidationError>());
    }

    public bool Delete(
        Guid id) {
        if(id == Guid.Empty) {
            return false;
        }

        return _store.Delete(id);
    }

    private static IReadOnlyList<BodyMeasurementValidationError> Validate(
        BodyMeasurementEntry entry,
        DateOnly currentDate) {
        var errors = new List<BodyMeasurementValidationError>();

        if(entry.Id == Guid.Empty) {
            errors.Add(BodyMeasurementValidationError.MissingId);
        }

        if(entry.Date > currentDate) {
            errors.Add(BodyMeasurementValidationError.FutureDate);
        }

        if(entry.WeightKg <= 0m) {
            errors.Add(BodyMeasurementValidationError.InvalidWeight);
        }

        if(!IsValidOptionalPercentage(entry.BodyFatPercent)) {
            errors.Add(BodyMeasurementValidationError.InvalidBodyFatPercent);
        }

        if(entry.BoneMassKg is decimal boneMassKg && (boneMassKg <= 0m || boneMassKg > entry.WeightKg)) {
            errors.Add(BodyMeasurementValidationError.InvalidBoneMass);
        }

        if(entry.MuscleMassKg is decimal muscleMassKg
            && (muscleMassKg <= 0m || muscleMassKg > entry.WeightKg)) {
            errors.Add(BodyMeasurementValidationError.InvalidMuscleMass);
        }

        if(!IsValidOptionalPercentage(entry.MusclePercent)) {
            errors.Add(
                BodyMeasurementValidationError
                    .InvalidMusclePercent);
        }

        if(!BodyMeasurementMuscleValueNormalizer.AreValuesConsistent(entry)) {
            errors.Add(BodyMeasurementValidationError.InconsistentMuscleValues);
        }

        var compositionResult = BodyCompositionConsistencyCalculator.Evaluate(entry);

        if(!compositionResult.IsConsistent) {
            errors.Add(BodyMeasurementValidationError.InconsistentBodyComposition);
        }

        return errors.Distinct().ToArray();
    }

    private static bool IsValidOptionalPercentage(
        decimal? value) {
        return value is null
            || value is > 0m and < 100m;
    }
}