namespace CalorieLedger.Domain.Nutrition;

public sealed record NutritionFacts(
    NutritionBasis Basis,
    decimal? CaloriesKcal,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG) {
    public static NutritionFacts Empty(NutritionBasis basis) {
        return new NutritionFacts(
            Basis: basis,
            CaloriesKcal: null,
            ProteinG: null,
            FatG: null,
            CarbsG: null);
    }
}