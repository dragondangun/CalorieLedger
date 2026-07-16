using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Adaptive;

public static class AdaptiveEnergyAdjustmentCalculator {
    public const decimal DefaultMinimumAbsoluteToleranceKg =
        0.1m;

    public const decimal DefaultRelativeTolerance =
        0.25m;

    public const decimal DefaultMaximumAdjustmentKcal =
        150m;

    public const decimal DefaultMinimumAdjustmentKcal =
        25m;

    public static AdaptiveEnergyAdjustmentResult Calculate(
        AdaptivePlanDataQualityResult dataQuality,
        NutritionGoal goal,
        decimal currentTargetCaloriesKcal,
        decimal minimumAbsoluteToleranceKg =
            DefaultMinimumAbsoluteToleranceKg,
        decimal relativeTolerance =
            DefaultRelativeTolerance,
        decimal maximumAdjustmentKcal =
            DefaultMaximumAdjustmentKcal,
        decimal minimumAdjustmentKcal =
            DefaultMinimumAdjustmentKcal) {
        ArgumentNullException.ThrowIfNull(
            dataQuality);

        ArgumentNullException.ThrowIfNull(
            goal);

        ValidateSettings(
            currentTargetCaloriesKcal,
            minimumAbsoluteToleranceKg,
            relativeTolerance,
            maximumAdjustmentKcal,
            minimumAdjustmentKcal);

        if(!dataQuality.IsSufficient
            || dataQuality.AverageDailyCaloriesKcal
                is null
            || dataQuality.WeightTrend
                .EstimatedWeeklyChangeKg
                is null) {
            return CreateUnavailableResult(
                status:
                    AdaptiveEnergyAdjustmentStatus
                        .InsufficientData,
                dataQuality:
                    dataQuality,
                currentTargetCaloriesKcal:
                    currentTargetCaloriesKcal);
        }

        if(goal.Strategy is null) {
            throw new ArgumentException(
                "Nutrition goal must have an energy strategy.",
                nameof(goal));
        }

        var averageDailyCalories =
            dataQuality
                .AverageDailyCaloriesKcal
                .Value;

        var observedWeeklyChange =
            dataQuality
                .WeightTrend
                .EstimatedWeeklyChangeKg
                .Value;

        var estimatedMaintenance =
            averageDailyCalories
            - observedWeeklyChange
            * EnergyStrategyCalculator
                .KcalPerKgBodyWeight
            / 7m;

        if(estimatedMaintenance <= 0m) {
            return CreateEstimateUnavailableResult(
                dataQuality,
                currentTargetCaloriesKcal,
                averageDailyCalories,
                observedWeeklyChange);
        }

        EnergyStrategyCalculation
            personalizedStrategyCalculation;

        try {
            personalizedStrategyCalculation =
                EnergyStrategyCalculator.Calculate(
                    strategy:
                        goal.Strategy,
                    goalType:
                        goal.GoalType,
                    maintenanceCaloriesKcal:
                        estimatedMaintenance);
        }
        catch(ArgumentException) {
            return CreateEstimateUnavailableResult(
                dataQuality,
                currentTargetCaloriesKcal,
                averageDailyCalories,
                observedWeeklyChange);
        }

        var targetWeeklyChange =
            personalizedStrategyCalculation
                .PredictedWeightChangeKgPerWeek;

        var personalizedTargetCalories =
            personalizedStrategyCalculation
                .TargetCaloriesKcal;

        if(personalizedTargetCalories <= 0m) {
            return CreateEstimateUnavailableResult(
                dataQuality,
                currentTargetCaloriesKcal,
                averageDailyCalories,
                observedWeeklyChange);
        }

        var weeklyChangeTolerance =
            Math.Max(
                minimumAbsoluteToleranceKg,
                Math.Abs(targetWeeklyChange)
                * relativeTolerance);

        var weeklyChangeDifference =
            Math.Abs(
                observedWeeklyChange
                - targetWeeklyChange);

        if(weeklyChangeDifference
            <= weeklyChangeTolerance) {
            return new AdaptiveEnergyAdjustmentResult(
                Status:
                    AdaptiveEnergyAdjustmentStatus
                        .WithinTolerance,
                DataQuality:
                    dataQuality,
                CurrentTargetCaloriesKcal:
                    currentTargetCaloriesKcal,
                AverageDailyCaloriesKcal:
                    averageDailyCalories,
                EstimatedMaintenanceCaloriesKcal:
                    Math.Round(
                        estimatedMaintenance,
                        0),
                ObservedWeeklyWeightChangeKg:
                    observedWeeklyChange,
                TargetWeeklyWeightChangeKg:
                    targetWeeklyChange,
                WeeklyChangeToleranceKg:
                    weeklyChangeTolerance,
                PersonalizedTargetCaloriesKcal:
                    Math.Round(
                        personalizedTargetCalories,
                        0),
                RecommendedDailyAdjustmentKcal:
                    null,
                RecommendedTargetCaloriesKcal:
                    null);
        }

        var rawDailyAdjustment =
            personalizedTargetCalories
            - currentTargetCaloriesKcal;

        if(Math.Abs(rawDailyAdjustment)
            < minimumAdjustmentKcal) {
            return new AdaptiveEnergyAdjustmentResult(
                Status:
                    AdaptiveEnergyAdjustmentStatus
                        .CurrentTargetAlreadySuitable,
                DataQuality:
                    dataQuality,
                CurrentTargetCaloriesKcal:
                    currentTargetCaloriesKcal,
                AverageDailyCaloriesKcal:
                    averageDailyCalories,
                EstimatedMaintenanceCaloriesKcal:
                    Math.Round(
                        estimatedMaintenance,
                        0),
                ObservedWeeklyWeightChangeKg:
                    observedWeeklyChange,
                TargetWeeklyWeightChangeKg:
                    targetWeeklyChange,
                WeeklyChangeToleranceKg:
                    weeklyChangeTolerance,
                PersonalizedTargetCaloriesKcal:
                    Math.Round(
                        personalizedTargetCalories,
                        0),
                RecommendedDailyAdjustmentKcal:
                    null,
                RecommendedTargetCaloriesKcal:
                    null);
        }

        var recommendedAdjustment =
            Math.Clamp(
                rawDailyAdjustment,
                -maximumAdjustmentKcal,
                maximumAdjustmentKcal);

        var recommendedTargetCalories =
            currentTargetCaloriesKcal
            + recommendedAdjustment;

        return new AdaptiveEnergyAdjustmentResult(
            Status:
                AdaptiveEnergyAdjustmentStatus
                    .RecommendationAvailable,
            DataQuality:
                dataQuality,
            CurrentTargetCaloriesKcal:
                currentTargetCaloriesKcal,
            AverageDailyCaloriesKcal:
                averageDailyCalories,
            EstimatedMaintenanceCaloriesKcal:
                Math.Round(
                    estimatedMaintenance,
                    0),
            ObservedWeeklyWeightChangeKg:
                observedWeeklyChange,
            TargetWeeklyWeightChangeKg:
                targetWeeklyChange,
            WeeklyChangeToleranceKg:
                weeklyChangeTolerance,
            PersonalizedTargetCaloriesKcal:
                Math.Round(
                    personalizedTargetCalories,
                    0),
            RecommendedDailyAdjustmentKcal:
                Math.Round(
                    recommendedAdjustment,
                    0),
            RecommendedTargetCaloriesKcal:
                Math.Round(
                    recommendedTargetCalories,
                    0));
    }

    private static AdaptiveEnergyAdjustmentResult
        CreateUnavailableResult(
            AdaptiveEnergyAdjustmentStatus status,
            AdaptivePlanDataQualityResult dataQuality,
            decimal currentTargetCaloriesKcal) {
        return new AdaptiveEnergyAdjustmentResult(
            Status:
                status,
            DataQuality:
                dataQuality,
            CurrentTargetCaloriesKcal:
                currentTargetCaloriesKcal,
            AverageDailyCaloriesKcal:
                dataQuality.AverageDailyCaloriesKcal,
            EstimatedMaintenanceCaloriesKcal:
                null,
            ObservedWeeklyWeightChangeKg:
                dataQuality.WeightTrend
                    .EstimatedWeeklyChangeKg,
            TargetWeeklyWeightChangeKg:
                null,
            WeeklyChangeToleranceKg:
                null,
            PersonalizedTargetCaloriesKcal:
                null,
            RecommendedDailyAdjustmentKcal:
                null,
            RecommendedTargetCaloriesKcal:
                null);
    }

    private static AdaptiveEnergyAdjustmentResult
        CreateEstimateUnavailableResult(
            AdaptivePlanDataQualityResult dataQuality,
            decimal currentTargetCaloriesKcal,
            decimal averageDailyCalories,
            decimal observedWeeklyChange) {
        return new AdaptiveEnergyAdjustmentResult(
            Status:
                AdaptiveEnergyAdjustmentStatus
                    .EstimateUnavailable,
            DataQuality:
                dataQuality,
            CurrentTargetCaloriesKcal:
                currentTargetCaloriesKcal,
            AverageDailyCaloriesKcal:
                averageDailyCalories,
            EstimatedMaintenanceCaloriesKcal:
                null,
            ObservedWeeklyWeightChangeKg:
                observedWeeklyChange,
            TargetWeeklyWeightChangeKg:
                null,
            WeeklyChangeToleranceKg:
                null,
            PersonalizedTargetCaloriesKcal:
                null,
            RecommendedDailyAdjustmentKcal:
                null,
            RecommendedTargetCaloriesKcal:
                null);
    }

    private static void ValidateSettings(
        decimal currentTargetCaloriesKcal,
        decimal minimumAbsoluteToleranceKg,
        decimal relativeTolerance,
        decimal maximumAdjustmentKcal,
        decimal minimumAdjustmentKcal) {
        if(currentTargetCaloriesKcal <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(currentTargetCaloriesKcal),
                currentTargetCaloriesKcal,
                "Current target calories must be greater than zero.");
        }

        if(minimumAbsoluteToleranceKg < 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(minimumAbsoluteToleranceKg));
        }

        if(relativeTolerance < 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(relativeTolerance));
        }

        if(maximumAdjustmentKcal <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAdjustmentKcal));
        }

        if(minimumAdjustmentKcal < 0m
            || minimumAdjustmentKcal
                > maximumAdjustmentKcal) {
            throw new ArgumentOutOfRangeException(
                nameof(minimumAdjustmentKcal));
        }
    }
}