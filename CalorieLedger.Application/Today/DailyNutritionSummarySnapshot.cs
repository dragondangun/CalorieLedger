using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Today;

public sealed record DailyNutritionSummarySnapshot(
    DateOnly Date,
    NutritionTotals ConsumedTotals);