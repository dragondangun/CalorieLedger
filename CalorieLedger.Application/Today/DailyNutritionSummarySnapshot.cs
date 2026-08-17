using CalorieLedger.Domain.Nutrition;
using System;

namespace CalorieLedger.Application.Today;

public sealed record DailyNutritionSummarySnapshot(
    DateOnly Date,
    NutritionTotals ConsumedTotals,
    bool IsEnergyComplete,
    bool AreMacrosComplete
);
