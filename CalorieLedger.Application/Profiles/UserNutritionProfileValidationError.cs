namespace CalorieLedger.Application.Profiles;

public enum UserNutritionProfileValidationError {
    MissingId,
    ProfileIdMismatch,
    MissingDisplayName,
    InvalidSex,
    InvalidAge,
    InvalidHeight,
    InvalidLifestyleActivityLevel,
}