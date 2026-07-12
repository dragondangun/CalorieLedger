using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public static class NutritionGoalDraftMapper {
    public static NutritionGoalDraft FromGoal(NutritionGoal goal) {
        return new NutritionGoalDraft(
            GoalType: goal.GoalType,
            TargetWeightKg: goal.TargetWeightKg,
            TargetBodyFatPercent:
                goal.TargetBodyFatPercent,
            TargetMuscleMassKg:
                goal.TargetMuscleMassKg,
            TargetMusclePercent:
                goal.TargetMusclePercent,
            DesiredWeightChangeKgPerWeek:
                goal.DesiredWeightChangeKgPerWeek,
            EnergyBalancePercent:
                goal.EnergyBalancePercent,
            StopAtBodyFatPercent:
                goal.StopAtBodyFatPercent,
            MassGainIntent:
                goal.MassGainIntent);
    }

    public static NutritionGoal ToGoal(NutritionGoalDraft draft) {
        return new NutritionGoal(
            GoalType: draft.GoalType,
            TargetWeightKg: draft.TargetWeightKg,
            TargetBodyFatPercent:
                draft.TargetBodyFatPercent,
            TargetMuscleMassKg:
                draft.TargetMuscleMassKg,
            TargetMusclePercent:
                draft.TargetMusclePercent,
            DesiredWeightChangeKgPerWeek:
                draft.DesiredWeightChangeKgPerWeek,
            EnergyBalancePercent:
                draft.EnergyBalancePercent,
            StopAtBodyFatPercent:
                draft.StopAtBodyFatPercent,
            MassGainIntent:
                draft.MassGainIntent);
    }
}