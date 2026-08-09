using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class BodyMeasurementHistoryService {
    private readonly IBodyMeasurementStore store;

    public BodyMeasurementHistoryService(IBodyMeasurementStore store) {
        ArgumentNullException.ThrowIfNull(store);

        this.store = store;
    }

    public IReadOnlyList<BodyMeasurementEntry> GetAll() {
        return store.GetAll();
    }

    public BodyMeasurementSaveResult Save(
        BodyMeasurementEntry entry,
        DateOnly currentDate
    ) {
        ArgumentNullException.ThrowIfNull(entry);

        var errors = Validate(entry, currentDate).ToList();

        if(HasConflictingDate(entry)) {
            errors.Add(
                BodyMeasurementValidationError.DuplicateDate
            );
        }

        if(errors.Count > 0) {
            return new BodyMeasurementSaveResult(
                IsSuccess: false,
                Errors: errors
            );
        }

        var normalizedEntry = BodyMeasurementMuscleValueNormalizer.Normalize(entry);

        store.Save(normalizedEntry);

        return new BodyMeasurementSaveResult(
            IsSuccess: true,
            Errors: Array.Empty<BodyMeasurementValidationError>()
        );
    }

    public bool Delete(
        Guid id) {
        if(id == Guid.Empty) {
            return false;
        }

        return store.Delete(id);
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

    private static bool IsValidOptionalPercentage(decimal? value) {
        return value is null || value is > 0m and < 100m;
    }

    public BodyMeasurementEntry? GetByDate(DateOnly date) {
        var measurements = GetAll();

        foreach(var measurement in measurements) {
            if(measurement.Date == date) {
                return measurement;
            }
        }

        return null;
    }

    private bool HasConflictingDate(BodyMeasurementEntry entry) {
        var measurements = store.GetAll();

        foreach(var existingEntry in measurements) {
            if(existingEntry.Id != entry.Id
                && existingEntry.Date == entry.Date) {
                return true;
            }
        }

        return false;
    }

    public BodyMeasurementHistorySnapshot GetSnapshot(DateOnly currentDate) {
        return new BodyMeasurementHistorySnapshot(
            asOfDate: currentDate,
            allMeasurements: GetAll()
        );
    }
}
