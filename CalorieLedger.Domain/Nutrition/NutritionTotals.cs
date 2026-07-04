namespace CalorieLedger.Domain.Nutrition;

// итог для конкретного количества
public sealed record NutritionTotals(
    decimal? CaloriesKcal,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG) {
    public static NutritionTotals Empty => new(
        CaloriesKcal: null,
        ProteinG: null,
        FatG: null,
        CarbsG: null);

    public static NutritionTotals Zero => new(
        CaloriesKcal: 0,
        ProteinG: 0,
        FatG: 0,
        CarbsG: 0);
}