namespace CalorieLedger.Application.History;

public sealed record WeeklyJournalSummarySnapshot(
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    DateOnly AvailableEndDate,
    int AvailableDayCount,
    int EnergyCompleteDayCount,
    int MacroCompleteDayCount,
    decimal? AverageFoodCaloriesKcal,
    decimal? AverageExtraActivityBurnedCaloriesKcal,
    decimal? AverageActivityAdjustedCaloriesKcal,
    decimal TotalExtraActivityBurnedCaloriesKcal,
    int WeightMeasurementCount,
    decimal? FirstWeightKg,
    decimal? LastWeightKg,
    decimal? WeightChangeKg
);
