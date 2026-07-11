namespace CalorieLedger.Domain.Profile;

public static class NutritionGoalStopEvaluator {
    public static GoalStopEvaluation Evaluate(
        BodyProfile body,
        NutritionGoal goal) {
        if(goal.StopAtBodyFatPercent is null) {
            return new GoalStopEvaluation(
                Status: GoalStopStatus.NotConfigured,
                CurrentBodyFatPercent: body.BodyFatPercent,
                StopAtBodyFatPercent: null);
        }

        if(goal.GoalType != WeightGoalType.GainWeight) {
            return new GoalStopEvaluation(
                Status: GoalStopStatus.NotApplicable,
                CurrentBodyFatPercent: body.BodyFatPercent,
                StopAtBodyFatPercent: goal.StopAtBodyFatPercent);
        }

        ValidateBodyFatPercent(
            goal.StopAtBodyFatPercent.Value,
            nameof(goal.StopAtBodyFatPercent));

        if(body.BodyFatPercent is null) {
            return new GoalStopEvaluation(
                Status: GoalStopStatus.MeasurementMissing,
                CurrentBodyFatPercent: null,
                StopAtBodyFatPercent: goal.StopAtBodyFatPercent);
        }

        ValidateBodyFatPercent(
            body.BodyFatPercent.Value,
            nameof(body.BodyFatPercent));

        var status =
            body.BodyFatPercent.Value >= goal.StopAtBodyFatPercent.Value
                ? GoalStopStatus.Reached
                : GoalStopStatus.NotReached;

        return new GoalStopEvaluation(
            Status: status,
            CurrentBodyFatPercent: body.BodyFatPercent,
            StopAtBodyFatPercent: goal.StopAtBodyFatPercent);
    }

    private static void ValidateBodyFatPercent(
        decimal value,
        string parameterName) {
        if(value is <= 0m or >= 100m) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Body fat percentage must be greater than 0 and less than 100.");
        }
    }
}