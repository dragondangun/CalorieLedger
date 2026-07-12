namespace CalorieLedger.Domain.Profile;

public static class NutritionGoalValidator {
    public static NutritionGoalValidationResult Validate(
        NutritionGoal goal) {
        var errors =
            new List<NutritionGoalValidationError>();

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
        var hasUnifiedStrategy =
        goal.Strategy is not null;

        var hasWeeklyWeightChange =
        goal.DesiredWeightChangeKgPerWeek is not null;

        var hasEnergyBalancePercent =
        goal.EnergyBalancePercent is not null;

        if(hasUnifiedStrategy) {
            if(hasWeeklyWeightChange
                || hasEnergyBalancePercent) {
                errors.Add(
                    NutritionGoalValidationError
                        .ConflictingLegacyAndUnifiedEnergyStrategies);
            }

            ValidateUnifiedEnergyStrategy(
                goal,
                errors);

            return;
        }

        if(hasWeeklyWeightChange
            && hasEnergyBalancePercent) {
            errors.Add(
                NutritionGoalValidationError
                    .ConflictingEnergyStrategies);
        }

        if(goal.DesiredWeightChangeKgPerWeek is <= 0m) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidDesiredWeightChange);
        }

        if(goal.EnergyBalancePercent is <= -100m) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidEnergyBalancePercent);
        }

        switch(goal.GoalType) {
            case WeightGoalType.LoseWeight:
                ValidateWeightLossStrategy(
                    goal,
                    hasWeeklyWeightChange,
                    hasEnergyBalancePercent,
                    errors);
                break;

            case WeightGoalType.GainWeight:
                ValidateWeightGainStrategy(
                    goal,
                    hasWeeklyWeightChange,
                    hasEnergyBalancePercent,
                    errors);
                break;

            case WeightGoalType.Maintain:
                ValidateMaintenanceStrategy(
                    goal,
                    errors);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(goal.GoalType),
                    goal.GoalType,
                    null);
        }
    }

    private static void ValidateUnifiedEnergyStrategy(
    NutritionGoal goal,
    ICollection<NutritionGoalValidationError> errors) {
        var strategy = goal.Strategy!;

        if(strategy.Value < 0m) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidEnergyStrategyValue);

            return;
        }

        if(strategy.Mode == EnergyStrategyMode.BalancePercent
            && strategy.Value >= 100m) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidEnergyStrategyValue);
        }

        if(goal.GoalType == WeightGoalType.Maintain) {
            if(strategy.Value != 0m) {
                errors.Add(
                    NutritionGoalValidationError
                        .MaintenanceRequiresNeutralEnergyBalance);
            }

            return;
        }

        if(strategy.Value == 0m) {
            errors.Add(
                NutritionGoalValidationError
                    .InvalidEnergyStrategyValue);
        }
    }

    private static void ValidateWeightLossStrategy(
        NutritionGoal goal,
        bool hasWeeklyWeightChange,
        bool hasEnergyBalancePercent,
        ICollection<NutritionGoalValidationError> errors) {
        if(!hasWeeklyWeightChange
            && !hasEnergyBalancePercent) {
            errors.Add(
                NutritionGoalValidationError
                    .MissingEnergyStrategy);
        }

        if(goal.EnergyBalancePercent is >= 0m) {
            errors.Add(
                NutritionGoalValidationError
                    .WeightLossRequiresDeficit);
        }
    }

    private static void ValidateWeightGainStrategy(
        NutritionGoal goal,
        bool hasWeeklyWeightChange,
        bool hasEnergyBalancePercent,
        ICollection<NutritionGoalValidationError> errors) {
        if(!hasWeeklyWeightChange
            && !hasEnergyBalancePercent) {
            errors.Add(
                NutritionGoalValidationError
                    .MissingEnergyStrategy);
        }

        if(goal.EnergyBalancePercent is <= 0m) {
            errors.Add(
                NutritionGoalValidationError
                    .WeightGainRequiresSurplus);
        }
    }

    private static void ValidateMaintenanceStrategy(
        NutritionGoal goal,
        ICollection<NutritionGoalValidationError> errors) {
        if(goal.DesiredWeightChangeKgPerWeek is not null) {
            errors.Add(
                NutritionGoalValidationError
                    .WeightChangeNotAllowedForMaintenance);
        }

        if(goal.EnergyBalancePercent is not null
            && goal.EnergyBalancePercent != 0m) {
            errors.Add(
                NutritionGoalValidationError
                    .MaintenanceRequiresNeutralEnergyBalance);
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