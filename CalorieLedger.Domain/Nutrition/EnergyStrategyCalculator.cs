using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Nutrition;

public static class EnergyStrategyCalculator {
    private const decimal KcalPerKgBodyWeight = 7700m;

    public static EnergyStrategyCalculation Calculate(
        EnergyStrategy strategy,
        WeightGoalType goalType,
        decimal maintenanceCaloriesKcal) {
        ArgumentNullException.ThrowIfNull(strategy);

        if(maintenanceCaloriesKcal <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(maintenanceCaloriesKcal),
                maintenanceCaloriesKcal,
                "Maintenance calories must be greater than zero.");
        }

        if(strategy.Value < 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(strategy),
                strategy.Value,
                "Energy strategy value cannot be negative.");
        }

        if(goalType != WeightGoalType.Maintain
            && strategy.Value == 0m) {
            throw new ArgumentException(
                "Weight loss and weight gain strategies must be greater than zero.",
                nameof(strategy));
        }

        if(strategy.Mode == EnergyStrategyMode.BalancePercent
            && strategy.Value >= 100m) {
            throw new ArgumentOutOfRangeException(
                nameof(strategy),
                strategy.Value,
                "Energy balance percentage must be less than 100.");
        }

        if(goalType == WeightGoalType.Maintain
            && strategy.Value != 0m) {
            throw new ArgumentException(
                "Maintenance strategy value must be zero.",
                nameof(strategy));
        }

        var direction = GetDirection(goalType);

        return strategy.Mode switch
        {
            EnergyStrategyMode.BalancePercent =>
                CalculateFromBalancePercent(
                    maintenanceCaloriesKcal,
                    strategy.Value,
                    direction),

            EnergyStrategyMode.WeightChangePerWeek =>
                CalculateFromWeightChange(
                    maintenanceCaloriesKcal,
                    strategy.Value,
                    direction),

            _ => throw new ArgumentOutOfRangeException(
                nameof(strategy.Mode),
                strategy.Mode,
                null)
        };
    }

    private static EnergyStrategyCalculation
        CalculateFromBalancePercent(
            decimal maintenanceCaloriesKcal,
            decimal balancePercentMagnitude,
            decimal direction) {
        var signedBalancePercent =
            balancePercentMagnitude * direction;

        var dailyEnergyAdjustment =
            maintenanceCaloriesKcal
            * signedBalancePercent
            / 100m;

        var predictedWeightChange =
            dailyEnergyAdjustment
            * 7m
            / KcalPerKgBodyWeight;

        return new EnergyStrategyCalculation(
            MaintenanceCaloriesKcal: maintenanceCaloriesKcal,
            DailyEnergyAdjustmentKcal: dailyEnergyAdjustment,
            EnergyBalancePercent: signedBalancePercent,
            PredictedWeightChangeKgPerWeek: predictedWeightChange);
    }

    private static EnergyStrategyCalculation
        CalculateFromWeightChange(
            decimal maintenanceCaloriesKcal,
            decimal weightChangeMagnitudeKgPerWeek,
            decimal direction) {
        var signedWeightChange =
            weightChangeMagnitudeKgPerWeek
            * direction;

        var dailyEnergyAdjustment =
            signedWeightChange
            * KcalPerKgBodyWeight
            / 7m;

        var energyBalancePercent =
            dailyEnergyAdjustment
            / maintenanceCaloriesKcal
            * 100m;

        return new EnergyStrategyCalculation(
            MaintenanceCaloriesKcal: maintenanceCaloriesKcal,
            DailyEnergyAdjustmentKcal: dailyEnergyAdjustment,
            EnergyBalancePercent: energyBalancePercent,
            PredictedWeightChangeKgPerWeek: signedWeightChange);
    }

    private static decimal GetDirection(
        WeightGoalType goalType) {
        return goalType switch
        {
            WeightGoalType.LoseWeight => -1m,
            WeightGoalType.Maintain => 0m,
            WeightGoalType.GainWeight => 1m,

            _ => throw new ArgumentOutOfRangeException(
                nameof(goalType),
                goalType,
                null)
        };
    }
}