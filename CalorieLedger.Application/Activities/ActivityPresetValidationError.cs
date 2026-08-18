namespace CalorieLedger.Application.Activities;

public enum ActivityPresetValidationError {
    MissingCode = 1,
    MissingName = 2,
    InvalidMetValue = 3,
    DuplicateName = 4,
    BuiltInPresetCannotBeChanged = 5
}
