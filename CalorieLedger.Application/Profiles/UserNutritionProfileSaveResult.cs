namespace CalorieLedger.Application.Profiles;

public sealed record UserNutritionProfileSaveResult(
    bool IsSuccess,
    IReadOnlyList<UserNutritionProfileValidationError> Errors
);