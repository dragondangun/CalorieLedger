using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed record NutritionGoalUpdateResult(
    bool IsSuccess,
    IReadOnlyList<NutritionGoalValidationError> Errors);