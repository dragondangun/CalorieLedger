using System;

namespace CalorieLedger.ViewModels.History;

public sealed record WeeklyTrendChartPoint(
    DateOnly WeekStartDate,
    string Label,
    decimal? FoodCaloriesKcal,
    decimal? AdjustedCaloriesKcal,
    decimal? WeightKg,
    bool IsPartialWeek,
    bool IsSelectedWeek
);
