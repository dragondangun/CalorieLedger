namespace CalorieLedger.Application.Profiles;

public enum BodyMeasurementValidationError {
    MissingId,
    FutureDate,
    InvalidWeight,
    InvalidBodyFatPercent,
    InvalidBoneMass,
    InvalidMuscleMass,
    InvalidMusclePercent,
    InconsistentMuscleValues
}