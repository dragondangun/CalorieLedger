namespace CalorieLedger.Domain.Nutrition;

public sealed record EnergyStrategyCalculation(
    decimal MaintenanceCaloriesKcal,
    decimal DailyEnergyAdjustmentKcal,
    decimal EnergyBalancePercent,
    decimal PredictedWeightChangeKgPerWeek) {
    public decimal TargetCaloriesKcal =>
        MaintenanceCaloriesKcal
        + DailyEnergyAdjustmentKcal;
}