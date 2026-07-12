namespace CalorieLedger.Application.Profiles;

public sealed record NutritionGoalStrategyPreview(
    decimal MaintenanceCaloriesKcal,
    decimal TargetCaloriesKcal,
    decimal EnergyBalancePercent,
    decimal PredictedWeightChangeKgPerWeek);