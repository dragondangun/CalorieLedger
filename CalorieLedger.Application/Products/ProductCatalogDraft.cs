using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Products;

public sealed record ProductCatalogDraft(
    Guid Id,
    string Name,
    NutritionBasis NutritionBasis,
    decimal? CaloriesKcal,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG,
    string? Brand = null,
    string? Barcode = null
);
