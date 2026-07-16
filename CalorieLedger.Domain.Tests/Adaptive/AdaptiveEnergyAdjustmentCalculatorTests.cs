using CalorieLedger.Domain.Adaptive;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Adaptive;

public sealed class
    AdaptiveEnergyAdjustmentCalculatorTests {
    [Fact]
    public void Calculate_WeightLossTooSlow_ReturnsCappedDecrease() {
        var quality =
            CreateSufficientQuality(
                averageCaloriesKcal: 2200m,
                observedWeeklyChangeKg: -0.2m);

        var goal = new NutritionGoal(
            GoalType:
                WeightGoalType.LoseWeight,
            Strategy:
                EnergyStrategy
                    .FromWeightChangePerWeek(
                        0.5m));

        var result =
            AdaptiveEnergyAdjustmentCalculator
                .Calculate(
                    quality,
                    goal,
                    currentTargetCaloriesKcal:
                        2100m);

        Assert.Equal(
            AdaptiveEnergyAdjustmentStatus
                .RecommendationAvailable,
            result.Status);

        Assert.Equal(
            2420m,
            result.EstimatedMaintenanceCaloriesKcal);

        Assert.Equal(
            1870m,
            result.PersonalizedTargetCaloriesKcal);

        Assert.Equal(
            -150m,
            result.RecommendedDailyAdjustmentKcal);

        Assert.Equal(
            1950m,
            result.RecommendedTargetCaloriesKcal);
    }

    [Fact]
    public void Calculate_WeightChangeWithinTolerance_ReturnsNoRecommendation() {
        var quality =
            CreateSufficientQuality(
                averageCaloriesKcal: 2000m,
                observedWeeklyChangeKg:
                    -0.42m);

        var goal = new NutritionGoal(
            GoalType:
                WeightGoalType.LoseWeight,
            Strategy:
                EnergyStrategy
                    .FromWeightChangePerWeek(
                        0.5m));

        var result =
            AdaptiveEnergyAdjustmentCalculator
                .Calculate(
                    quality,
                    goal,
                    currentTargetCaloriesKcal:
                        2000m);

        Assert.Equal(
            AdaptiveEnergyAdjustmentStatus
                .WithinTolerance,
            result.Status);

        Assert.False(
            result.HasRecommendation);

        Assert.Null(
            result.RecommendedTargetCaloriesKcal);
    }

    [Fact]
    public void Calculate_CurrentTargetAlreadyMatchesEstimate_ReturnsSuitableStatus() {
        var quality =
            CreateSufficientQuality(
                averageCaloriesKcal: 2200m,
                observedWeeklyChangeKg:
                    -0.2m);

        var goal = new NutritionGoal(
            GoalType:
                WeightGoalType.LoseWeight,
            Strategy:
                EnergyStrategy
                    .FromWeightChangePerWeek(
                        0.5m));

        var result =
            AdaptiveEnergyAdjustmentCalculator
                .Calculate(
                    quality,
                    goal,
                    currentTargetCaloriesKcal:
                        1870m);

        Assert.Equal(
            AdaptiveEnergyAdjustmentStatus
                .CurrentTargetAlreadySuitable,
            result.Status);

        Assert.False(
            result.HasRecommendation);

        Assert.Null(
            result.RecommendedDailyAdjustmentKcal);
    }

    [Fact]
    public void Calculate_InsufficientData_ReturnsNoRecommendation() {
        var quality =
            CreateInsufficientQuality();

        var goal = new NutritionGoal(
            GoalType:
                WeightGoalType.LoseWeight,
            Strategy:
                EnergyStrategy
                    .FromBalancePercent(
                        15m));

        var result =
            AdaptiveEnergyAdjustmentCalculator
                .Calculate(
                    quality,
                    goal,
                    currentTargetCaloriesKcal:
                        2000m);

        Assert.Equal(
            AdaptiveEnergyAdjustmentStatus
                .InsufficientData,
            result.Status);

        Assert.False(
            result.HasRecommendation);

        Assert.Null(
            result.EstimatedMaintenanceCaloriesKcal);
    }

    [Fact]
    public void Calculate_MaintenanceWeightGain_ReturnsCappedDecrease() {
        var quality =
            CreateSufficientQuality(
                averageCaloriesKcal: 2500m,
                observedWeeklyChangeKg:
                    0.3m);

        var goal = new NutritionGoal(
            GoalType:
                WeightGoalType.Maintain,
            Strategy:
                EnergyStrategy
                    .FromBalancePercent(
                        0m));

        var result =
            AdaptiveEnergyAdjustmentCalculator
                .Calculate(
                    quality,
                    goal,
                    currentTargetCaloriesKcal:
                        2500m);

        Assert.Equal(
            AdaptiveEnergyAdjustmentStatus
                .RecommendationAvailable,
            result.Status);

        Assert.Equal(
            2170m,
            result.EstimatedMaintenanceCaloriesKcal);

        Assert.Equal(
            -150m,
            result.RecommendedDailyAdjustmentKcal);

        Assert.Equal(
            2350m,
            result.RecommendedTargetCaloriesKcal);
    }

    private static
        AdaptivePlanDataQualityResult
        CreateSufficientQuality(
            decimal averageCaloriesKcal,
            decimal observedWeeklyChangeKg) {
        var startDate =
            new DateOnly(2026, 7, 1);

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
                    observedWeeklyChangeKg);

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
                averageCaloriesKcal,
            WeightTrend:
                trend,
            Issues:
                AdaptivePlanDataIssue.None);
    }

    private static
        AdaptivePlanDataQualityResult
        CreateInsufficientQuality() {
        var startDate =
            new DateOnly(2026, 7, 1);

        var endDate =
            startDate.AddDays(13);

        var trend =
            new BodyWeightTrendResult(
                Status:
                    BodyWeightTrendStatus
                        .InsufficientData,
                WindowStartDate:
                    startDate,
                WindowEndDate:
                    endDate,
                MeasurementDayCount:
                    4,
                AverageWeightKg:
                    79m,
                EstimatedWeeklyChangeKg:
                    null);

        return new AdaptivePlanDataQualityResult(
            WindowStartDate:
                startDate,
            WindowEndDate:
                endDate,
            ObservationDaySpan:
                14,
            WeightMeasurementDayCount:
                4,
            CompleteIntakeDayCount:
                6,
            AverageDailyCaloriesKcal:
                2000m,
            WeightTrend:
                trend,
            Issues:
                AdaptivePlanDataIssue
                    .InsufficientWeightMeasurementDays
                | AdaptivePlanDataIssue
                    .InsufficientCompleteIntakeDays);
    }
}