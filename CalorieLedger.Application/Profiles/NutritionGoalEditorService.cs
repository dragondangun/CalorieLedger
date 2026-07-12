using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class NutritionGoalEditorService(
    IUserNutritionProfileProvider profileProvider,
    NutritionGoalUpdateService goalUpdateService) {
    public NutritionGoalDraft LoadCurrentGoal() {
        var profile =
            profileProvider.GetCurrentProfile();

        return NutritionGoalDraftMapper.FromGoal(
            profile.Goal);
    }

    public NutritionGoalDraft CreateNewGoal(WeightGoalType goalType) {
        return new NutritionGoalDraft(
            GoalType: goalType,
            StrategyMode:
                EnergyStrategyMode.BalancePercent,
            StrategyValue:
                goalType == WeightGoalType.Maintain
                    ? 0m
                    : null);
    }

    public NutritionGoalStrategyPreview? CalculateStrategyPreview(
    NutritionGoalDraft draft) {
        if(draft.StrategyValue is null
            || draft.StrategyValue < 0m
            || (draft.GoalType != WeightGoalType.Maintain
                && draft.StrategyValue == 0m)) {
            return null;
        }

        if(draft.StrategyMode == EnergyStrategyMode.BalancePercent
            && draft.StrategyValue >= 100m) {
            return null;
        }

        var profile = profileProvider.GetCurrentProfile();

        var maintenanceCalories = NutritionTargetCalculator.CalculateMaintenanceCalories(profile);

        try {
            var strategy = new EnergyStrategy(
                Mode: draft.StrategyMode,
                Value: draft.StrategyValue.Value);

            var calculation = EnergyStrategyCalculator.Calculate(
                strategy: strategy,
                goalType: draft.GoalType,
                maintenanceCaloriesKcal: maintenanceCalories);

            return new NutritionGoalStrategyPreview(
                MaintenanceCaloriesKcal: calculation.MaintenanceCaloriesKcal,
                TargetCaloriesKcal: calculation.TargetCaloriesKcal,
                EnergyBalancePercent: calculation.EnergyBalancePercent,
                PredictedWeightChangeKgPerWeek: calculation.PredictedWeightChangeKgPerWeek);
        }
        catch(ArgumentException) {
            return null;
        }
    }

    public NutritionGoalUpdateResult Save(NutritionGoalDraft draft) {
        var goal = NutritionGoalDraftMapper.ToGoal(draft);

        return goalUpdateService.UpdateGoal(goal);
    }
}