namespace CalorieLedger.ViewModels.History;

public sealed record WeeklyTrendChartPoint(
    string Label,
    decimal? FoodCaloriesKcal,
    decimal? AdjustedCaloriesKcal,
    decimal? WeightKg,
    bool IsPartialWeek
);
