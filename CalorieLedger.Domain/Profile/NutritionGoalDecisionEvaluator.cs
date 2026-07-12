namespace CalorieLedger.Domain.Profile;

public static class NutritionGoalDecisionEvaluator {
    public static NutritionGoalDecision Evaluate(
        BodyProfile body,
        NutritionGoal goal) {
        var progress =
            NutritionGoalProgressEvaluator.Evaluate(body, goal);

        var stopCondition =
            NutritionGoalStopEvaluator.Evaluate(body, goal);

        var status = DetermineStatus(
            progress,
            stopCondition,
            goal.GoalType);

        var availableActions = GetAvailableActions(
            status,
            goal.GoalType);

        return new NutritionGoalDecision(
            Status: status,
            Progress: progress,
            StopCondition: stopCondition,
            AvailableActions: availableActions);
    }

    private static NutritionGoalDecisionStatus DetermineStatus(
        NutritionGoalProgressEvaluation progress,
        GoalStopEvaluation stopCondition,
        WeightGoalType goalType) {
        if(stopCondition.Status == GoalStopStatus.Reached) {
            return NutritionGoalDecisionStatus.StopLimitReached;
        }

        if(progress.Status == NutritionGoalProgressStatus.Reached) {
            return NutritionGoalDecisionStatus.GoalReached;
        }

        if(progress.Status == NutritionGoalProgressStatus.MeasurementMissing
            || stopCondition.Status == GoalStopStatus.MeasurementMissing) {
            return NutritionGoalDecisionStatus.MeasurementMissing;
        }

        if(progress.Status == NutritionGoalProgressStatus.PartiallyReached) {
            return NutritionGoalDecisionStatus.PartiallyReached;
        }

        if(progress.Status == NutritionGoalProgressStatus.InProgress) {
            return NutritionGoalDecisionStatus.InProgress;
        }

        if(progress.Status == NutritionGoalProgressStatus.NotConfigured
            && goalType == WeightGoalType.Maintain) {
            return NutritionGoalDecisionStatus.InProgress;
        }

        return NutritionGoalDecisionStatus.NotConfigured;
    }

    private static IReadOnlyList<GoalNextAction> GetAvailableActions(
        NutritionGoalDecisionStatus status,
        WeightGoalType goalType) {
        return status switch
        {
            NutritionGoalDecisionStatus.NotConfigured =>
            [
                GoalNextAction.SetNewGoal
            ],

            NutritionGoalDecisionStatus.MeasurementMissing =>
            [
                GoalNextAction.RepeatMeasurements,
                GoalNextAction.ContinueCurrentGoal,
                GoalNextAction.SetNewGoal
            ],

            NutritionGoalDecisionStatus.InProgress =>
            [
                GoalNextAction.ContinueCurrentGoal,
                GoalNextAction.SetNewGoal
            ],

            NutritionGoalDecisionStatus.PartiallyReached =>
            [
                GoalNextAction.ContinueCurrentGoal,
                GoalNextAction.SwitchToMaintenance,
                GoalNextAction.SetNewGoal
            ],

            NutritionGoalDecisionStatus.GoalReached =>
                GetReachedGoalActions(goalType),

            NutritionGoalDecisionStatus.StopLimitReached =>
            [
                GoalNextAction.SwitchToMaintenance,
                GoalNextAction.StartWeightLoss,
                GoalNextAction.SetNewGoal,
                GoalNextAction.ContinueCurrentGoal
            ],

            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                null)
        };
    }

    private static IReadOnlyList<GoalNextAction> GetReachedGoalActions(
        WeightGoalType goalType) {
        return goalType switch
        {
            WeightGoalType.LoseWeight =>
            [
                GoalNextAction.SwitchToMaintenance,
                GoalNextAction.StartWeightGain,
                GoalNextAction.SetNewGoal,
                GoalNextAction.ContinueCurrentGoal
            ],

            WeightGoalType.GainWeight =>
            [
                GoalNextAction.SwitchToMaintenance,
                GoalNextAction.StartWeightLoss,
                GoalNextAction.SetNewGoal,
                GoalNextAction.ContinueCurrentGoal
            ],

            WeightGoalType.Maintain =>
            [
                GoalNextAction.ContinueCurrentGoal,
                GoalNextAction.StartWeightLoss,
                GoalNextAction.StartWeightGain,
                GoalNextAction.SetNewGoal
            ],

            _ => throw new ArgumentOutOfRangeException(
                nameof(goalType),
                goalType,
                null)
        };
    }
}