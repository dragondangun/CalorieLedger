using CalorieLedger.Application.Adaptive;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class NutritionGoalUpdateService {
    private readonly IUserNutritionProfileStore profileStore;

    private readonly IAdaptiveEnergyHistoryResetter? adaptiveEnergyHistoryResetter;

    public NutritionGoalUpdateService(IUserNutritionProfileStore profileStore) : this(
            profileStore,
            adaptiveEnergyHistoryResetter: null
    ) { }

    public NutritionGoalUpdateService(
        IUserNutritionProfileStore profileStore,
        IAdaptiveEnergyHistoryResetter? adaptiveEnergyHistoryResetter
    ) {
        ArgumentNullException.ThrowIfNull(profileStore);

        this.profileStore = profileStore;

        this.adaptiveEnergyHistoryResetter = adaptiveEnergyHistoryResetter;
    }

    public NutritionGoalUpdateResult UpdateGoal(NutritionGoal goal) {
        ArgumentNullException.ThrowIfNull(goal);

        var validationResult = NutritionGoalValidator.Validate(goal);

        if(!validationResult.IsValid) {
            return new NutritionGoalUpdateResult(
                IsSuccess: false,
                Errors: validationResult.Errors);
        }

        var previousGoal = profileStore.GetCurrentProfile().Goal;

        var shouldResetAdaptiveHistory = AdaptiveEnergyHistoryResetPolicy.ShouldReset(
            previousGoal,
            goal);

        profileStore.UpdateGoal(goal);

        if(shouldResetAdaptiveHistory) {
            adaptiveEnergyHistoryResetter?.ResetHistory();
        }

        return new NutritionGoalUpdateResult(
            IsSuccess: true,
            Errors: Array.Empty<NutritionGoalValidationError>());
    }
}
