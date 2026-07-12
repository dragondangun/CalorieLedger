using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class NutritionGoalTransitionService(
    NutritionGoalUpdateService goalUpdateService) {
    public NutritionGoalUpdateResult SwitchToMaintenance() {
        var maintenanceGoal = new NutritionGoal(
            GoalType: WeightGoalType.Maintain,
            EnergyBalancePercent: 0m);

        return goalUpdateService.UpdateGoal(
            maintenanceGoal);
    }
}