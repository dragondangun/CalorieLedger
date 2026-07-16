namespace CalorieLedger.Domain.Nutrition;

public sealed record DailyEnergyIntakeEntry(
    DateOnly Date,
    decimal CaloriesKcal,
    bool IsComplete);