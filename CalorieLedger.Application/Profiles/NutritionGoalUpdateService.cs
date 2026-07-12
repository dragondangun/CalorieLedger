using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class NutritionGoalUpdateService(
    IUserNutritionProfileStore profileStore) {
    public NutritionGoalUpdateResult UpdateGoal(
        NutritionGoal goal) {
        var validation =
            NutritionGoalValidator.Validate(goal);

        if(!validation.IsValid) {
            return new NutritionGoalUpdateResult(
                IsSuccess: false,
                Errors: validation.Errors);
        }

        profileStore.UpdateGoal(goal);

        return new NutritionGoalUpdateResult(
            IsSuccess: true,
            Errors: Array.Empty<NutritionGoalValidationError>());
    }
}