using CalorieLedger.Application.Adaptive;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class NutritionGoalUpdateService {
    private readonly IUserNutritionProfileStore _profileStore;

    private readonly IAdaptiveEnergyHistoryResetter? _adaptiveEnergyHistoryResetter;

    public NutritionGoalUpdateService(IUserNutritionProfileStore profileStore)
        : this(
            profileStore,
            adaptiveEnergyHistoryResetter: null) {
    }

    public NutritionGoalUpdateService(
        IUserNutritionProfileStore profileStore,
        IAdaptiveEnergyHistoryResetter?
            adaptiveEnergyHistoryResetter) {
        ArgumentNullException.ThrowIfNull(profileStore);

        _profileStore = profileStore;

        _adaptiveEnergyHistoryResetter = adaptiveEnergyHistoryResetter;
    }

    public NutritionGoalUpdateResult UpdateGoal(NutritionGoal goal) {
        ArgumentNullException.ThrowIfNull(goal);

        var validationResult = NutritionGoalValidator.Validate(goal);

        if(!validationResult.IsValid) {
            return new NutritionGoalUpdateResult(
                IsSuccess: false,
                Errors: validationResult.Errors);
        }

        var previousGoal = _profileStore.GetCurrentProfile().Goal;

        var shouldResetAdaptiveHistory = AdaptiveEnergyHistoryResetPolicy.ShouldReset(
            previousGoal,
            goal);

        _profileStore.UpdateGoal(goal);

        if(shouldResetAdaptiveHistory) {
            _adaptiveEnergyHistoryResetter?.ResetHistory();
        }

        return new NutritionGoalUpdateResult(
            IsSuccess: true,
            Errors: Array.Empty<NutritionGoalValidationError>());
    }
}