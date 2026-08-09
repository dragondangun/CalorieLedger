using CalorieLedger.Domain.Adaptive;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;
using CalorieLedger.Application.Profiles;

namespace CalorieLedger.Application.Adaptive;

public sealed class AdaptiveEnergyAssessmentService:IAdaptiveEnergyHistoryResetter {
    private readonly IAdaptiveEnergyEvaluationStore evaluationStore;

    public AdaptiveEnergyAssessmentService(IAdaptiveEnergyEvaluationStore evaluationStore) {
        ArgumentNullException.ThrowIfNull(evaluationStore);

        this.evaluationStore = evaluationStore;
    }

    public AdaptiveEnergyAssessmentResult Evaluate(
        BodyMeasurementHistorySnapshot measurementSnapshot,
        IEnumerable<DailyEnergyIntakeEntry> intakeEntries,
        NutritionGoal goal,
        decimal currentTargetCaloriesKcal
    ) {
        ArgumentNullException.ThrowIfNull(measurementSnapshot);
        ArgumentNullException.ThrowIfNull(intakeEntries);
        ArgumentNullException.ThrowIfNull(goal);

        var intakeEntryArray = intakeEntries.ToArray();

        var evaluationDate = measurementSnapshot.AsOfDate;

        var dataQuality = AdaptivePlanDataQualityEvaluator.Evaluate(
            measurementSnapshot.EffectiveMeasurements,
            intakeEntryArray,
            asOfDate: evaluationDate
        );

        var adjustment = AdaptiveEnergyAdjustmentCalculator.Calculate(
            dataQuality,
            goal,
            currentTargetCaloriesKcal
        );

        var storedHistory = evaluationStore.GetAll().ToArray();

        if(storedHistory.Any(
            entry => entry.EvaluationDate > evaluationDate
        )) {
            throw new InvalidOperationException(
                "Adaptive evaluation history contains dates later than the current evaluation date."
            );
        }

        /*
         * Запись за текущую дату исключается.
         * Благодаря этому повторный расчёт в тот же
         * день заменяет запись, а не считается
         * второй последовательной проверкой.
         */
        var previousHistory = storedHistory.Where(
            entry => entry.EvaluationDate < evaluationDate
        ).ToArray();

        var recommendation = AdaptiveEnergyRecommendationEvaluator.Evaluate(
            adjustment,
            evaluationDate,
            previousHistory
        );

        if(recommendation.ShouldRecordEvaluation) {
            evaluationStore.Save(recommendation.CurrentEvaluationEntry!);
        }

        return new AdaptiveEnergyAssessmentResult(
            DataQuality: dataQuality,
            Adjustment: adjustment,
            Recommendation: recommendation
        );
    }

    public void ResetHistory() {
        evaluationStore.Clear();
    }
}
