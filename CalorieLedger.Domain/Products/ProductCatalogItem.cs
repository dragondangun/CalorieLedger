using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Products;

// запись в базе продуктов
public sealed record ProductCatalogItem(
    Guid Id,
    string Name,
    NutritionFacts Nutrition,
    string? Brand = null,
    string? Barcode = null);