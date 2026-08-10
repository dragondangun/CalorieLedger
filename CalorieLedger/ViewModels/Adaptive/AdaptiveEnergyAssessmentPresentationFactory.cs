using CalorieLedger.Application.Adaptive;
using CalorieLedger.Domain.Adaptive;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;
using System;
using System.Globalization;

namespace CalorieLedger.ViewModels.Adaptive;

public static class AdaptiveEnergyAssessmentPresentationFactory {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static AdaptiveEnergyAssessmentPresentation Create(
        AdaptiveEnergyAssessmentResult result,
        NutritionGoal goal
    ) {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(goal);

        return result.Adjustment.Status switch {
            AdaptiveEnergyAdjustmentStatus.InsufficientData => CreateInsufficientData(
                result.DataQuality
            ),

            AdaptiveEnergyAdjustmentStatus.EstimateUnavailable => new AdaptiveEnergyAssessmentPresentation(
                State: AdaptiveEnergyAssessmentState.Unavailable,
                Details: "Данных достаточно, но по ним пока нельзя получить устойчивую оценку энергобаланса."
            ),

            AdaptiveEnergyAdjustmentStatus.WithinTolerance => new AdaptiveEnergyAssessmentPresentation(
                State: AdaptiveEnergyAssessmentState.WithinTarget,
                Details: FormatTrendDetails(
                    result.Adjustment
                )
            ),

            AdaptiveEnergyAdjustmentStatus.CurrentTargetAlreadySuitable => new AdaptiveEnergyAssessmentPresentation(
                State: AdaptiveEnergyAssessmentState.ObservationRequired,
                Details: FormatTrendDetails(
                    result.Adjustment
                ),
                Recommendation: "Расчётная корректировка слишком мала, чтобы менять текущую энергетическую стратегию. Продолжите наблюдение."
            ),

            AdaptiveEnergyAdjustmentStatus.RecommendationAvailable => CreateRecommendation(
                result,
                goal
            ),

            _ => throw new ArgumentOutOfRangeException(
                nameof(result),
                result.Adjustment.Status,
                null
            )
        };
    }

    private static AdaptiveEnergyAssessmentPresentation CreateInsufficientData(
        AdaptivePlanDataQualityResult dataQuality
    ) {
        var details = $"Период наблюдения: {dataQuality.ObservationDaySpan}/{AdaptivePlanDataQualityEvaluator.DefaultMinimumObservationDays} дн.; дней с измерением веса: {dataQuality.WeightMeasurementDayCount}/{AdaptivePlanDataQualityEvaluator.DefaultMinimumWeightMeasurementDays}; полных дней питания: {dataQuality.CompleteIntakeDayCount}/{AdaptivePlanDataQualityEvaluator.DefaultMinimumCompleteIntakeDays}.";

        if(dataQuality.HasIssue(
            AdaptivePlanDataIssue.WeightTrendUnavailable
        )) {
            details += " Тренд веса пока не удалось оценить.";
        }

        return new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.Unavailable,
            Details: details
        );
    }

    private static AdaptiveEnergyAssessmentPresentation CreateRecommendation(
        AdaptiveEnergyAssessmentResult result,
        NutritionGoal goal
    ) {
        return result.Recommendation.Status switch {
            AdaptiveEnergyRecommendationStatus.EvaluationTooSoon => new AdaptiveEnergyAssessmentPresentation(
                State: AdaptiveEnergyAssessmentState.ObservationRequired,
                Details: FormatTrendDetails(
                    result.Adjustment
                ),
                Recommendation: FormatTooSoonRecommendation(
                    result.Recommendation
                )
            ),

            AdaptiveEnergyRecommendationStatus.AwaitingConsistentDeviation => new AdaptiveEnergyAssessmentPresentation(
                State: AdaptiveEnergyAssessmentState.ObservationRequired,
                Details:
                    $"{FormatTrendDetails(result.Adjustment)} " +
                    $"Подтверждений отклонения: {result.Recommendation.ConsecutiveDeviationCount}/{result.Recommendation.RequiredConsecutiveDeviationCount}.",
                Recommendation: "Продолжите наблюдение до следующей независимой оценки."
            ),

            AdaptiveEnergyRecommendationStatus.RecommendationAvailable => CreateAdjustmentSuggested(
                result,
                goal
            ),

            AdaptiveEnergyRecommendationStatus.NoRecommendation =>
                throw new InvalidOperationException(
                    "An available adaptive adjustment must produce an observation or recommendation state."
                ),

            _ => throw new ArgumentOutOfRangeException(
                nameof(result),
                result.Recommendation.Status,
                null
            )
        };
    }

    private static AdaptiveEnergyAssessmentPresentation CreateAdjustmentSuggested(
        AdaptiveEnergyAssessmentResult result,
        NutritionGoal goal
    ) {
        var adjustment = result.Adjustment;

        var recommendedTargetCalories = adjustment.RecommendedTargetCaloriesKcal ?? throw new InvalidOperationException(
            "An available recommendation must include target calories."
        );

        var recommendedDailyAdjustment = adjustment.RecommendedDailyAdjustmentKcal
            ?? throw new InvalidOperationException(
                "An available recommendation must include a daily calorie adjustment."
            );

        return new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.AdjustmentSuggested,
            Details: FormatTrendDetails(adjustment),
            Recommendation:
                $"Изменить дневную норму на {FormatSignedCalories(recommendedDailyAdjustment)} ккал: с {FormatCalories(adjustment.CurrentTargetCaloriesKcal)} до {FormatCalories(recommendedTargetCalories)} ккал/день.",
            SuggestedStrategy: CreateSuggestedStrategy(
                goal,
                adjustment
            )
        );
    }

    private static AdaptiveEnergyStrategySuggestion? CreateSuggestedStrategy(
        NutritionGoal goal,
        AdaptiveEnergyAdjustmentResult adjustment
    ) {
        if(goal.GoalType == WeightGoalType.Maintain
            || goal.Strategy is null
            || adjustment.EstimatedMaintenanceCaloriesKcal is not decimal estimatedMaintenanceCalories
            || adjustment.RecommendedTargetCaloriesKcal is not decimal recommendedTargetCalories) {
            return null;
        }

        var signedDailyAdjustment = recommendedTargetCalories - estimatedMaintenanceCalories;

        var hasGoalCompatibleDirection = goal.GoalType switch {
            WeightGoalType.LoseWeight => signedDailyAdjustment < 0m,
            WeightGoalType.GainWeight => signedDailyAdjustment > 0m,
            WeightGoalType.Maintain => false,

            _ => throw new ArgumentOutOfRangeException(
                nameof(goal.GoalType),
                goal.GoalType,
                null
            )
        };

        if(!hasGoalCompatibleDirection) {
            return null;
        }

        var suggestedValue = goal.Strategy.Mode switch {
            EnergyStrategyMode.BalancePercent =>
                Math.Round(
                    Math.Abs(signedDailyAdjustment)
                    / estimatedMaintenanceCalories
                    * 100m,
                    1
                ),

            EnergyStrategyMode.WeightChangePerWeek =>
                Math.Round(
                    Math.Abs(signedDailyAdjustment)
                    * 7m
                    / EnergyStrategyCalculator.KcalPerKgBodyWeight,
                    2
                ),

            _ => throw new ArgumentOutOfRangeException(
                nameof(goal.Strategy.Mode),
                goal.Strategy.Mode,
                null
            )
        };

        if(suggestedValue <= 0m
            || (goal.Strategy.Mode == EnergyStrategyMode.BalancePercent
                && suggestedValue >= 100m)
        ) {
            return null;
        }

        return new AdaptiveEnergyStrategySuggestion(
            Mode: goal.Strategy.Mode,
            Value: suggestedValue
        );
    }

    private static string FormatTrendDetails(AdaptiveEnergyAdjustmentResult adjustment) {
        return $"Фактический темп: {FormatSignedWeightChange(adjustment.ObservedWeeklyWeightChangeKg)}; целевой: {FormatSignedWeightChange(adjustment.TargetWeeklyWeightChangeKg)}.";
    }

    private static string FormatTooSoonRecommendation(AdaptiveEnergyRecommendationResult recommendation) {
        if(recommendation.DaysSincePreviousEvaluation is not int daysSincePreviousEvaluation) {
            return "Продолжите наблюдение до следующей независимой оценки.";
        }

        var remainingDays = Math.Max(
            1,
            AdaptiveEnergyRecommendationEvaluator.DefaultMinimumDaysBetweenEvaluations
            - daysSincePreviousEvaluation
        );

        return $"Следующую независимую оценку стоит проводить не раньше чем через {remainingDays} дн.";
    }

    private static string FormatSignedWeightChange(decimal? value) {
        if(value is null) {
            return "нет оценки";
        }

        var sign = value.Value switch {
            > 0m => "+",
            < 0m => "−",
            _ => string.Empty
        };

        return $"{sign}{Math.Abs(value.Value).ToString("0.00", RussianCulture)} кг/нед.";
    }

    private static string FormatSignedCalories(decimal value) {
        var sign = value switch {
            > 0m => "+",
            < 0m => "−",
            _ => string.Empty
        };

        return $"{sign}{Math.Abs(value).ToString("0", RussianCulture)}";
    }

    private static string FormatCalories(decimal value) {
        return value.ToString(
            "0",
            RussianCulture
        );
    }
}
