using CalorieLedger.Domain.Adaptive;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Adaptive;

public sealed class
    AdaptiveEnergyRecommendationEvaluatorTests {
    [Fact]
    public void Evaluate_FirstDeviation_WaitsForSecondCheck() {
        var evaluationDate =
            new DateOnly(2026, 8, 1);

        var adjustment =
            CreateCandidateAdjustment(
                recommendedAdjustmentKcal:
                    -150m);

        var result =
            AdaptiveEnergyRecommendationEvaluator
                .Evaluate(
                    adjustment,
                    evaluationDate,
                    history:
                        Array.Empty<
                            AdaptiveEnergyEvaluationEntry>());

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus
                .AwaitingConsistentDeviation,
            result.Status);

        Assert.False(
            result.HasRecommendation);

        Assert.True(
            result.ShouldRecordEvaluation);

        Assert.Equal(
            1,
            result.ConsecutiveDeviationCount);

        Assert.Equal(
            AdaptiveEnergyDeviationDirection
                .DecreaseCalories,
            result.CurrentEvaluationEntry!
                .DeviationDirection);
    }

    [Fact]
    public void Evaluate_SecondMatchingDeviation_ReturnsRecommendation() {
        var firstDate =
            new DateOnly(2026, 8, 1);

        var firstAdjustment =
            CreateCandidateAdjustment(
                recommendedAdjustmentKcal:
                    -150m);

        var history =
            new[]
            {
                AdaptiveEnergyEvaluationEntry
                    .FromResult(
                        firstDate,
                        firstAdjustment)
            };

        var currentAdjustment =
            CreateCandidateAdjustment(
                recommendedAdjustmentKcal:
                    -100m);

        var result =
            AdaptiveEnergyRecommendationEvaluator
                .Evaluate(
                    currentAdjustment,
                    evaluationDate:
                        firstDate.AddDays(7),
                    history:
                        history);

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus
                .RecommendationAvailable,
            result.Status);

        Assert.True(
            result.HasRecommendation);

        Assert.Equal(
            2,
            result.ConsecutiveDeviationCount);

        Assert.Equal(
            7,
            result.DaysSincePreviousEvaluation);
    }

    [Fact]
    public void Evaluate_OppositeDeviationDirection_RestartsSequence() {
        var firstDate =
            new DateOnly(2026, 8, 1);

        var history =
            new[]
            {
                AdaptiveEnergyEvaluationEntry
                    .FromResult(
                        firstDate,
                        CreateCandidateAdjustment(
                            recommendedAdjustmentKcal:
                                -150m))
            };

        var result =
            AdaptiveEnergyRecommendationEvaluator
                .Evaluate(
                    CreateCandidateAdjustment(
                        recommendedAdjustmentKcal:
                            100m),
                    evaluationDate:
                        firstDate.AddDays(7),
                    history:
                        history);

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus
                .AwaitingConsistentDeviation,
            result.Status);

        Assert.Equal(
            1,
            result.ConsecutiveDeviationCount);

        Assert.Equal(
            AdaptiveEnergyDeviationDirection
                .IncreaseCalories,
            result.CurrentEvaluationEntry!
                .DeviationDirection);
    }

    [Fact]
    public void Evaluate_NoRecommendationBreaksDeviationSequence() {
        var startDate =
            new DateOnly(2026, 8, 1);

        var history =
            new[]
            {
                AdaptiveEnergyEvaluationEntry
                    .FromResult(
                        startDate,
                        CreateCandidateAdjustment(
                            recommendedAdjustmentKcal:
                                -150m)),

                AdaptiveEnergyEvaluationEntry
                    .FromResult(
                        startDate.AddDays(7),
                        CreateNoAdjustment(
                            AdaptiveEnergyAdjustmentStatus
                                .WithinTolerance))
            };

        var result =
            AdaptiveEnergyRecommendationEvaluator
                .Evaluate(
                    CreateCandidateAdjustment(
                        recommendedAdjustmentKcal:
                            -100m),
                    evaluationDate:
                        startDate.AddDays(14),
                    history:
                        history);

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus
                .AwaitingConsistentDeviation,
            result.Status);

        Assert.Equal(
            1,
            result.ConsecutiveDeviationCount);
    }

    [Fact]
    public void Evaluate_BeforeSevenDays_ReturnsTooSoon() {
        var firstDate =
            new DateOnly(2026, 8, 1);

        var history =
            new[]
            {
                AdaptiveEnergyEvaluationEntry
                    .FromResult(
                        firstDate,
                        CreateCandidateAdjustment(
                            recommendedAdjustmentKcal:
                                -150m))
            };

        var result =
            AdaptiveEnergyRecommendationEvaluator
                .Evaluate(
                    CreateCandidateAdjustment(
                        recommendedAdjustmentKcal:
                            -100m),
                    evaluationDate:
                        firstDate.AddDays(5),
                    history:
                        history);

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus
                .EvaluationTooSoon,
            result.Status);

        Assert.False(
            result.ShouldRecordEvaluation);

        Assert.Null(
            result.CurrentEvaluationEntry);

        Assert.Equal(
            5,
            result.DaysSincePreviousEvaluation);
    }

    [Fact]
    public void Evaluate_AfterLongGap_StartsNewSequence() {
        var firstDate =
            new DateOnly(2026, 8, 1);

        var history =
            new[]
            {
                AdaptiveEnergyEvaluationEntry
                    .FromResult(
                        firstDate,
                        CreateCandidateAdjustment(
                            recommendedAdjustmentKcal:
                                -150m))
            };

        var result =
            AdaptiveEnergyRecommendationEvaluator
                .Evaluate(
                    CreateCandidateAdjustment(
                        recommendedAdjustmentKcal:
                            -100m),
                    evaluationDate:
                        firstDate.AddDays(15),
                    history:
                        history);

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus
                .AwaitingConsistentDeviation,
            result.Status);

        Assert.Equal(
            1,
            result.ConsecutiveDeviationCount);
    }

    private static AdaptiveEnergyAdjustmentResult
        CreateCandidateAdjustment(
            decimal recommendedAdjustmentKcal) {
        const decimal currentTargetCalories =
            2000m;

        return new AdaptiveEnergyAdjustmentResult(
            Status:
                AdaptiveEnergyAdjustmentStatus
                    .RecommendationAvailable,
            DataQuality:
                CreateSufficientDataQuality(),
            CurrentTargetCaloriesKcal:
                currentTargetCalories,
            AverageDailyCaloriesKcal:
                2100m,
            EstimatedMaintenanceCaloriesKcal:
                2300m,
            ObservedWeeklyWeightChangeKg:
                -0.2m,
            TargetWeeklyWeightChangeKg:
                -0.5m,
            WeeklyChangeToleranceKg:
                0.125m,
            PersonalizedTargetCaloriesKcal:
                currentTargetCalories
                + recommendedAdjustmentKcal,
            RecommendedDailyAdjustmentKcal:
                recommendedAdjustmentKcal,
            RecommendedTargetCaloriesKcal:
                currentTargetCalories
                + recommendedAdjustmentKcal);
    }

    private static AdaptiveEnergyAdjustmentResult
        CreateNoAdjustment(
            AdaptiveEnergyAdjustmentStatus status) {
        return new AdaptiveEnergyAdjustmentResult(
            Status:
                status,
            DataQuality:
                CreateSufficientDataQuality(),
            CurrentTargetCaloriesKcal:
                2000m,
            AverageDailyCaloriesKcal:
                2100m,
            EstimatedMaintenanceCaloriesKcal:
                2200m,
            ObservedWeeklyWeightChangeKg:
                -0.48m,
            TargetWeeklyWeightChangeKg:
                -0.5m,
            WeeklyChangeToleranceKg:
                0.125m,
            PersonalizedTargetCaloriesKcal:
                1650m,
            RecommendedDailyAdjustmentKcal:
                null,
            RecommendedTargetCaloriesKcal:
                null);
    }

    private static AdaptivePlanDataQualityResult
        CreateSufficientDataQuality() {
        var startDate =
            new DateOnly(2026, 7, 19);

        var endDate =
            startDate.AddDays(13);

        var trend =
            new BodyWeightTrendResult(
                Status:
                    BodyWeightTrendStatus.Available,
                WindowStartDate:
                    startDate,
                WindowEndDate:
                    endDate,
                MeasurementDayCount:
                    8,
                AverageWeightKg:
                    79m,
                EstimatedWeeklyChangeKg:
                    -0.2m);

        return new AdaptivePlanDataQualityResult(
            WindowStartDate:
                startDate,
            WindowEndDate:
                endDate,
            ObservationDaySpan:
                14,
            WeightMeasurementDayCount:
                8,
            CompleteIntakeDayCount:
                10,
            AverageDailyCaloriesKcal:
                2100m,
            WeightTrend:
                trend,
            Issues:
                AdaptivePlanDataIssue.None);
    }
}