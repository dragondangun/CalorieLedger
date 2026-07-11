namespace CalorieLedger.Domain.Profile;

public static class NutritionGoalProgressEvaluator {
    private const decimal MaintenanceTolerance = 0.1m;

    public static NutritionGoalProgressEvaluation Evaluate(
        BodyProfile body,
        NutritionGoal goal) {
        var weightStatus = EvaluateWeight(
            currentWeightKg: body.WeightKg,
            targetWeightKg: goal.TargetWeightKg,
            goalType: goal.GoalType);

        var bodyFatStatus = EvaluateBodyFat(
            currentBodyFatPercent: body.BodyFatPercent,
            targetBodyFatPercent: goal.TargetBodyFatPercent,
            goalType: goal.GoalType);

        var muscleMassStatus = EvaluateMinimumTarget(
            currentValue: body.MuscleMassKg,
            targetValue: goal.TargetMuscleMassKg);

        var musclePercentStatus = EvaluateMusclePercent(
            currentMusclePercent: body.MusclePercent,
            targetMusclePercent: goal.TargetMusclePercent);

        var overallStatus = CalculateOverallStatus(
        [
            weightStatus,
            bodyFatStatus,
            muscleMassStatus,
            musclePercentStatus
        ]);

        return new NutritionGoalProgressEvaluation(
            Status: overallStatus,
            WeightStatus: weightStatus,
            BodyFatStatus: bodyFatStatus,
            MuscleMassStatus: muscleMassStatus,
            MusclePercentStatus: musclePercentStatus);
    }

    private static GoalMetricStatus EvaluateWeight(
        decimal currentWeightKg,
        decimal? targetWeightKg,
        WeightGoalType goalType) {
        if(targetWeightKg is null) {
            return GoalMetricStatus.NotConfigured;
        }

        if(targetWeightKg.Value <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(targetWeightKg),
                targetWeightKg,
                "Target weight must be greater than zero.");
        }

        return EvaluateDirectionalTarget(
            currentValue: currentWeightKg,
            targetValue: targetWeightKg.Value,
            goalType: goalType);
    }

    private static GoalMetricStatus EvaluateBodyFat(
        decimal? currentBodyFatPercent,
        decimal? targetBodyFatPercent,
        WeightGoalType goalType) {
        if(targetBodyFatPercent is null) {
            return GoalMetricStatus.NotConfigured;
        }

        ValidatePercentage(
            targetBodyFatPercent.Value,
            nameof(targetBodyFatPercent));

        if(currentBodyFatPercent is null) {
            return GoalMetricStatus.MeasurementMissing;
        }

        ValidatePercentage(
            currentBodyFatPercent.Value,
            nameof(currentBodyFatPercent));

        return EvaluateDirectionalTarget(
            currentValue: currentBodyFatPercent.Value,
            targetValue: targetBodyFatPercent.Value,
            goalType: goalType);
    }

    private static GoalMetricStatus EvaluateMusclePercent(
        decimal? currentMusclePercent,
        decimal? targetMusclePercent) {
        if(targetMusclePercent is null) {
            return GoalMetricStatus.NotConfigured;
        }

        ValidatePercentage(
            targetMusclePercent.Value,
            nameof(targetMusclePercent));

        if(currentMusclePercent is null) {
            return GoalMetricStatus.MeasurementMissing;
        }

        ValidatePercentage(
            currentMusclePercent.Value,
            nameof(currentMusclePercent));

        return currentMusclePercent.Value >= targetMusclePercent.Value
            ? GoalMetricStatus.Reached
            : GoalMetricStatus.NotReached;
    }

    private static GoalMetricStatus EvaluateMinimumTarget(
        decimal? currentValue,
        decimal? targetValue) {
        if(targetValue is null) {
            return GoalMetricStatus.NotConfigured;
        }

        if(targetValue.Value <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(targetValue),
                targetValue,
                "Target value must be greater than zero.");
        }

        if(currentValue is null) {
            return GoalMetricStatus.MeasurementMissing;
        }

        return currentValue.Value >= targetValue.Value
            ? GoalMetricStatus.Reached
            : GoalMetricStatus.NotReached;
    }

    private static GoalMetricStatus EvaluateDirectionalTarget(
        decimal currentValue,
        decimal targetValue,
        WeightGoalType goalType) {
        var reached = goalType switch
        {
            WeightGoalType.LoseWeight =>
                currentValue <= targetValue,

            WeightGoalType.GainWeight =>
                currentValue >= targetValue,

            WeightGoalType.Maintain =>
                Math.Abs(currentValue - targetValue) <= MaintenanceTolerance,

            _ => throw new ArgumentOutOfRangeException(
                nameof(goalType),
                goalType,
                null)
        };

        return reached
            ? GoalMetricStatus.Reached
            : GoalMetricStatus.NotReached;
    }

    private static NutritionGoalProgressStatus CalculateOverallStatus(
        IReadOnlyCollection<GoalMetricStatus> metricStatuses) {
        var configuredStatuses = metricStatuses
            .Where(status => status != GoalMetricStatus.NotConfigured)
            .ToArray();

        if(configuredStatuses.Length == 0) {
            return NutritionGoalProgressStatus.NotConfigured;
        }

        if(configuredStatuses.Any(
                status => status == GoalMetricStatus.MeasurementMissing)) {
            return NutritionGoalProgressStatus.MeasurementMissing;
        }

        if(configuredStatuses.All(
                status => status == GoalMetricStatus.Reached)) {
            return NutritionGoalProgressStatus.Reached;
        }

        if(configuredStatuses.Any(
                status => status == GoalMetricStatus.Reached)) {
            return NutritionGoalProgressStatus.PartiallyReached;
        }

        return NutritionGoalProgressStatus.InProgress;
    }

    private static void ValidatePercentage(
        decimal value,
        string parameterName) {
        if(value is <= 0m or >= 100m) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Percentage must be greater than 0 and less than 100.");
        }
    }
}