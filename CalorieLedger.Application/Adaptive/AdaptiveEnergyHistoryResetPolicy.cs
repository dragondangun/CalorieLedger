using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Adaptive;

public static class AdaptiveEnergyHistoryResetPolicy {
    public static bool ShouldReset(
        NutritionGoal previousGoal,
        NutritionGoal updatedGoal) {
        ArgumentNullException.ThrowIfNull(
            previousGoal);

        ArgumentNullException.ThrowIfNull(
            updatedGoal);

        if(previousGoal.GoalType
            != updatedGoal.GoalType) {
            return true;
        }

        return !Equals(
            previousGoal.Strategy,
            updatedGoal.Strategy);
    }
}