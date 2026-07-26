namespace CalorieLedger.Application.Profiles;

public enum BodyMeasurementValidationError {
    MissingId,
    FutureDate,
    DuplicateDate,
    InvalidWeight,
    InvalidBodyFatPercent,
    InvalidBoneMass,
    InvalidMuscleMass,
    InvalidMusclePercent,
    InconsistentMuscleValues,
    InconsistentBodyComposition
}