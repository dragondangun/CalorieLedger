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
            EnergyBalancePercent:
                goalType == WeightGoalType.Maintain
                    ? 0m
                    : null);
    }

    public NutritionGoalUpdateResult Save(NutritionGoalDraft draft) {
        var goal =
            NutritionGoalDraftMapper.ToGoal(draft);

        return goalUpdateService.UpdateGoal(goal);
    }
}