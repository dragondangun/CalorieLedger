namespace CalorieLedger.Domain.Nutrition;

public sealed record DailyNutritionTarget(
    decimal CaloriesKcal,
    decimal? ProteinG = null,
    decimal? FatG = null,
    decimal? CarbsG = null);