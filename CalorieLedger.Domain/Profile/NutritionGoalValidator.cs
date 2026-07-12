namespace CalorieLedger.Domain.Profile;

public static class NutritionGoalValidator {
    public static NutritionGoalValidationResult Validate(NutritionGoal goal) {
        ArgumentNullException.ThrowIfNull(goal);

        var errors = new List<NutritionGoalValidationError>();

        ValidateTargets(goal, errors);
        ValidateEnergyStrategy(goal, errors);
        ValidateWeightGainOptions(goal, errors);

        return new NutritionGoalValidationResult(
            Errors: errors
                .Distinct()
                .ToArray());
    }

    private static void ValidateTargets(
        NutritionGoal goal,
        ICollection<NutritionGoalValidationError> errors) {
        if(goal.TargetWeightKg is <= 0m) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidTargetWeight);
        }

        if(!IsValidOptionalPercentage(
                goal.TargetBodyFatPercent)) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidTargetBodyFatPercent);
        }

        if(goal.TargetMuscleMassKg is <= 0m) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidTargetMuscleMass);
        }

        if(!IsValidOptionalPercentage(
                goal.TargetMusclePercent)) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidTargetMusclePercent);
        }
    }

    private static void ValidateEnergyStrategy(
        NutritionGoal goal,
        ICollection<NutritionGoalValidationError> errors) {
        if(goal.Strategy is null) {
            errors.Add(NutritionGoalValidationError.MissingEnergyStrategy);

            return;
        }

        ValidateEnergyStrategyValue(
            goal,
            errors);
    }

    private static void ValidateEnergyStrategyValue(NutritionGoal goal,
        ICollection<NutritionGoalValidationError> errors) {
        var strategy = goal.Strategy!;

        if(!Enum.IsDefined(strategy.Mode)) {
            errors.Add(NutritionGoalValidationError.InvalidEnergyStrategyValue);

            return;
        }

        if(strategy.Value < 0m) {
            errors.Add(NutritionGoalValidationError.InvalidEnergyStrategyValue);

            return;
        }

        if(strategy.Mode == EnergyStrategyMode.BalancePercent
            && strategy.Value >= 100m) {
            errors.Add(NutritionGoalValidationError.InvalidEnergyStrategyValue);
        }

        if(goal.GoalType == WeightGoalType.Maintain) {
            if(strategy.Value != 0m) {
                errors.Add(NutritionGoalValidationError.MaintenanceRequiresNeutralEnergyBalance);
            }

            return;
        }

        if(strategy.Value == 0m) {
            errors.Add(NutritionGoalValidationError.InvalidEnergyStrategyValue);
        }
    }

    private static void ValidateWeightGainOptions(
        NutritionGoal goal,
        ICollection<NutritionGoalValidationError> errors) {
        if(!IsValidOptionalPercentage(
                goal.StopAtBodyFatPercent)) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidStopBodyFatPercent);
        }

        if(goal.StopAtBodyFatPercent is not null
            && goal.GoalType != WeightGoalType.GainWeight) {
            errors.Add(
                NutritionGoalValidationError
                    .StopBodyFatOnlyForWeightGain);
        }

        if(goal.MassGainIntent is not null
            && goal.GoalType != WeightGoalType.GainWeight) {
            errors.Add(
                NutritionGoalValidationError
                    .MassGainIntentOnlyForWeightGain);
        }
    }

    private static bool IsValidOptionalPercentage(
        decimal? value) {
        return value is null
            || value is > 0m and < 100m;
    }
}