namespace CalorieLedger.Domain.Profile;

public sealed record NutritionGoalValidationResult(
    IReadOnlyList<NutritionGoalValidationError> Errors) {
    public bool IsValid =>
        Errors.Count == 0;

    public bool HasError(
        NutritionGoalValidationError error) {
        return Errors.Contains(error);
    }
}