using CalorieLedger.Domain.Adaptive;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Adaptive;

public sealed class AdaptiveEnergyAssessmentService {
    private readonly
        IAdaptiveEnergyEvaluationStore
        _evaluationStore;

    public AdaptiveEnergyAssessmentService(
        IAdaptiveEnergyEvaluationStore
            evaluationStore) {
        ArgumentNullException.ThrowIfNull(
            evaluationStore);

        _evaluationStore =
            evaluationStore;
    }

    public AdaptiveEnergyAssessmentResult Evaluate(
        IEnumerable<BodyMeasurementEntry>
            bodyMeasurements,
        IEnumerable<DailyEnergyIntakeEntry>
            intakeEntries,
        NutritionGoal goal,
        decimal currentTargetCaloriesKcal,
        DateOnly evaluationDate) {
        ArgumentNullException.ThrowIfNull(
            bodyMeasurements);

        ArgumentNullException.ThrowIfNull(
            intakeEntries);

        ArgumentNullException.ThrowIfNull(
            goal);

        var bodyMeasurementArray =
            bodyMeasurements.ToArray();

        var intakeEntryArray =
            intakeEntries.ToArray();

        var dataQuality =
            AdaptivePlanDataQualityEvaluator
                .Evaluate(
                    bodyMeasurementArray,
                    intakeEntryArray,
                    asOfDate:
                        evaluationDate);

        var adjustment =
            AdaptiveEnergyAdjustmentCalculator
                .Calculate(
                    dataQuality,
                    goal,
                    currentTargetCaloriesKcal);

        var storedHistory =
            _evaluationStore
                .GetAll()
                .ToArray();

        if(storedHistory.Any(
                entry =>
                    entry.EvaluationDate
                    > evaluationDate)) {
            throw new InvalidOperationException(
                "Adaptive evaluation history contains dates later than the current evaluation date.");
        }

        /*
         * Запись за текущую дату исключается.
         * Благодаря этому повторный расчёт в тот же
         * день заменяет запись, а не считается
         * второй последовательной проверкой.
         */
        var previousHistory =
            storedHistory
                .Where(
                    entry =>
                        entry.EvaluationDate
                        < evaluationDate)
                .ToArray();

        var recommendation =
            AdaptiveEnergyRecommendationEvaluator
                .Evaluate(
                    adjustment,
                    evaluationDate,
                    previousHistory);

        if(recommendation
            .ShouldRecordEvaluation) {
            _evaluationStore.Save(
                recommendation
                    .CurrentEvaluationEntry!);
        }

        return new AdaptiveEnergyAssessmentResult(
            DataQuality:
                dataQuality,
            Adjustment:
                adjustment,
            Recommendation:
                recommendation);
    }

    public void ResetHistory() {
        _evaluationStore.Clear();
    }
}