namespace CalorieLedger.Application.Activities;

public enum ActivityValidationError {
    MissingId,
    FutureDate,
    MissingName,
    InvalidBurnedCalories,
    InvalidDuration
}
