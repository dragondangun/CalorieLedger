using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class NutritionGoalTransitionService(
    IUserNutritionProfileStore profileStore) {
    public void SwitchToMaintenance() {
        var maintenanceGoal = new NutritionGoal(
            GoalType: WeightGoalType.Maintain,
            EnergyBalancePercent: 0m);

        profileStore.UpdateGoal(maintenanceGoal);
    }
}