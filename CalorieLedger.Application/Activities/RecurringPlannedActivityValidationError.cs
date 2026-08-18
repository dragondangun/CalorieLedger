namespace CalorieLedger.Application.Activities;

public enum RecurringPlannedActivityValidationError {
    MissingId = 1,
    MissingName = 2,
    InvalidInterval = 3,
    InvalidDuration = 4,
    InvalidMetValue = 5,
    InvalidManualBurnedCalories = 6
}
