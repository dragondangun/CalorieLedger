namespace CalorieLedger.Application.Activities;

public enum PlannedActivityValidationError {
    MissingId = 1,
    MissingName = 2,
    InvalidDuration = 3,
    InvalidMetValue = 4,
    InvalidManualBurnedCalories = 5
}
