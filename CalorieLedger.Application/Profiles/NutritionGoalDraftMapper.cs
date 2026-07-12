using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public static class NutritionGoalDraftMapper {
    public static NutritionGoalDraft FromGoal(
    NutritionGoal goal) {
        var energyBalancePercent = goal.EnergyBalancePercent;

        var desiredWeightChangeKgPerWeek = goal.DesiredWeightChangeKgPerWeek;

        if(goal.Strategy is not null) {
            switch(goal.Strategy.Mode) {
                case EnergyStrategyMode.BalancePercent:
                    energyBalancePercent =ApplyGoalDirection(
                        goal.Strategy.Value,
                        goal.GoalType);

                    desiredWeightChangeKgPerWeek = null;
                    break;

                case EnergyStrategyMode.WeightChangePerWeek:
                    desiredWeightChangeKgPerWeek =
                        goal.Strategy.Value;

                    energyBalancePercent = null;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(goal.Strategy.Mode),
                        goal.Strategy.Mode,
                        null);
            }
        }

        return new NutritionGoalDraft(
            GoalType: goal.GoalType,
            TargetWeightKg: goal.TargetWeightKg,
            TargetBodyFatPercent: goal.TargetBodyFatPercent,
            TargetMuscleMassKg:goal.TargetMuscleMassKg,
            TargetMusclePercent: goal.TargetMusclePercent,
            DesiredWeightChangeKgPerWeek: desiredWeightChangeKgPerWeek,
            EnergyBalancePercent:energyBalancePercent,
            StopAtBodyFatPercent: goal.StopAtBodyFatPercent,
            MassGainIntent: goal.MassGainIntent);
    }

    private static decimal ApplyGoalDirection(
        decimal value,
        WeightGoalType goalType) {
        return goalType switch
        {
            WeightGoalType.LoseWeight => -value,
            WeightGoalType.Maintain => 0m,
            WeightGoalType.GainWeight => value,

            _ => throw new ArgumentOutOfRangeException(
                nameof(goalType),
                goalType,
                null)
        };
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