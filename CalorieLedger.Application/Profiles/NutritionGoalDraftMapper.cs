using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public static class NutritionGoalDraftMapper {
    public static NutritionGoalDraft FromGoal(
        NutritionGoal goal) {
        var strategy = ResolveStrategy(goal);

        return new NutritionGoalDraft(
            GoalType: goal.GoalType,
            TargetWeightKg: goal.TargetWeightKg,
            TargetBodyFatPercent:
                goal.TargetBodyFatPercent,
            TargetMuscleMassKg:
                goal.TargetMuscleMassKg,
            TargetMusclePercent:
                goal.TargetMusclePercent,
            StrategyMode: strategy.Mode,
            StrategyValue: strategy.Value,
            StopAtBodyFatPercent:
                goal.StopAtBodyFatPercent,
            MassGainIntent:
                goal.MassGainIntent);
    }

    public static NutritionGoal ToGoal(NutritionGoalDraft draft) {
        EnergyStrategy? strategy = null;

        if(draft.StrategyValue is not null) {
            strategy = new EnergyStrategy(
                Mode: draft.StrategyMode,
                Value: draft.StrategyValue.Value);
        }

        return new NutritionGoal(
            GoalType: draft.GoalType,
            TargetWeightKg: draft.TargetWeightKg,
            TargetBodyFatPercent: draft.TargetBodyFatPercent,
            TargetMuscleMassKg: draft.TargetMuscleMassKg,
            TargetMusclePercent: draft.TargetMusclePercent,
            StopAtBodyFatPercent: draft.StopAtBodyFatPercent,
            MassGainIntent: draft.MassGainIntent,
            Strategy: strategy);
    }

    private static (EnergyStrategyMode Mode, decimal? Value) ResolveStrategy(NutritionGoal goal) {
        if(goal.Strategy is not null) {
            return (
                goal.Strategy.Mode,
                goal.Strategy.Value);
        }

        return (
            EnergyStrategyMode.BalancePercent,
            goal.GoalType == WeightGoalType.Maintain
                ? 0m
                : null);
    }
}