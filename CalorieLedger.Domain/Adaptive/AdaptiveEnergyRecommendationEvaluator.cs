namespace CalorieLedger.Domain.Adaptive;

public static class
    AdaptiveEnergyRecommendationEvaluator {
    public const int
        DefaultRequiredConsecutiveDeviations = 2;

    public const int
        DefaultMinimumDaysBetweenEvaluations = 7;

    public const int
        DefaultMaximumDaysBetweenEvaluations = 14;

    public static AdaptiveEnergyRecommendationResult
        Evaluate(
            AdaptiveEnergyAdjustmentResult
                currentAdjustment,
            DateOnly evaluationDate,
            IEnumerable<AdaptiveEnergyEvaluationEntry>
                history,
            int requiredConsecutiveDeviations =
                DefaultRequiredConsecutiveDeviations,
            int minimumDaysBetweenEvaluations =
                DefaultMinimumDaysBetweenEvaluations,
            int maximumDaysBetweenEvaluations =
                DefaultMaximumDaysBetweenEvaluations) {
        ArgumentNullException.ThrowIfNull(
            currentAdjustment);

        ArgumentNullException.ThrowIfNull(
            history);

        ValidateSettings(
            requiredConsecutiveDeviations,
            minimumDaysBetweenEvaluations,
            maximumDaysBetweenEvaluations);

        var orderedHistory =
            history
                .OrderBy(
                    entry =>
                        entry.EvaluationDate)
                .ToArray();

        ValidateHistory(
            orderedHistory,
            evaluationDate);

        var previousEvaluation =
            orderedHistory.LastOrDefault();

        int? daysSincePreviousEvaluation =
            previousEvaluation is null
                ? null
                : evaluationDate.DayNumber
                  - previousEvaluation
                      .EvaluationDate
                      .DayNumber;

        if(daysSincePreviousEvaluation
            < minimumDaysBetweenEvaluations) {
            return new AdaptiveEnergyRecommendationResult(
                Status:
                    AdaptiveEnergyRecommendationStatus
                        .EvaluationTooSoon,
                CurrentAdjustment:
                    currentAdjustment,
                CurrentEvaluationEntry:
                    null,
                ConsecutiveDeviationCount:
                    0,
                RequiredConsecutiveDeviationCount:
                    requiredConsecutiveDeviations,
                DaysSincePreviousEvaluation:
                    daysSincePreviousEvaluation);
        }

        var currentEntry =
            AdaptiveEnergyEvaluationEntry.FromResult(
                evaluationDate,
                currentAdjustment);

        if(currentEntry.DeviationDirection
            == AdaptiveEnergyDeviationDirection.None) {
            return new AdaptiveEnergyRecommendationResult(
                Status:
                    AdaptiveEnergyRecommendationStatus
                        .NoRecommendation,
                CurrentAdjustment:
                    currentAdjustment,
                CurrentEvaluationEntry:
                    currentEntry,
                ConsecutiveDeviationCount:
                    0,
                RequiredConsecutiveDeviationCount:
                    requiredConsecutiveDeviations,
                DaysSincePreviousEvaluation:
                    daysSincePreviousEvaluation);
        }

        var consecutiveDeviationCount =
            CountConsecutiveDeviations(
                orderedHistory,
                currentEntry,
                minimumDaysBetweenEvaluations,
                maximumDaysBetweenEvaluations);

        var status =
            consecutiveDeviationCount
                >= requiredConsecutiveDeviations
                ? AdaptiveEnergyRecommendationStatus
                    .RecommendationAvailable
                : AdaptiveEnergyRecommendationStatus
                    .AwaitingConsistentDeviation;

        return new AdaptiveEnergyRecommendationResult(
            Status:
                status,
            CurrentAdjustment:
                currentAdjustment,
            CurrentEvaluationEntry:
                currentEntry,
            ConsecutiveDeviationCount:
                consecutiveDeviationCount,
            RequiredConsecutiveDeviationCount:
                requiredConsecutiveDeviations,
            DaysSincePreviousEvaluation:
                daysSincePreviousEvaluation);
    }

    private static int CountConsecutiveDeviations(
        IReadOnlyList<AdaptiveEnergyEvaluationEntry>
            history,
        AdaptiveEnergyEvaluationEntry currentEntry,
        int minimumDaysBetweenEvaluations,
        int maximumDaysBetweenEvaluations) {
        var count = 1;

        var laterEntry =
            currentEntry;

        for(var index =
                 history.Count - 1;
             index >= 0;
             index--) {
            var earlierEntry =
                history[index];

            var dayGap =
                laterEntry.EvaluationDate.DayNumber
                - earlierEntry
                    .EvaluationDate
                    .DayNumber;

            if(dayGap
                    < minimumDaysBetweenEvaluations
                || dayGap
                    > maximumDaysBetweenEvaluations) {
                break;
            }

            if(earlierEntry.DeviationDirection
                != currentEntry.DeviationDirection) {
                break;
            }

            count++;

            laterEntry =
                earlierEntry;
        }

        return count;
    }

    private static void ValidateHistory(
        IReadOnlyCollection<
            AdaptiveEnergyEvaluationEntry> history,
        DateOnly evaluationDate) {
        var hasDuplicateDates =
            history
                .GroupBy(
                    entry =>
                        entry.EvaluationDate)
                .Any(
                    group =>
                        group.Count() > 1);

        if(hasDuplicateDates) {
            throw new ArgumentException(
                "Only one adaptive energy evaluation is allowed per date.",
                nameof(history));
        }

        if(history.Any(
                entry =>
                    entry.EvaluationDate
                        >= evaluationDate)) {
            throw new ArgumentException(
                "Evaluation history must contain only dates earlier than the current evaluation date.",
                nameof(history));
        }
    }

    private static void ValidateSettings(
        int requiredConsecutiveDeviations,
        int minimumDaysBetweenEvaluations,
        int maximumDaysBetweenEvaluations) {
        if(requiredConsecutiveDeviations < 1) {
            throw new ArgumentOutOfRangeException(
                nameof(
                    requiredConsecutiveDeviations));
        }

        if(minimumDaysBetweenEvaluations < 1) {
            throw new ArgumentOutOfRangeException(
                nameof(
                    minimumDaysBetweenEvaluations));
        }

        if(maximumDaysBetweenEvaluations
            < minimumDaysBetweenEvaluations) {
            throw new ArgumentOutOfRangeException(
                nameof(
                    maximumDaysBetweenEvaluations),
                maximumDaysBetweenEvaluations,
                "Maximum interval cannot be shorter than the minimum interval.");
        }
    }
}