using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Fridge;

public sealed record FridgeItem(
    Guid Id,
    string Name,
    FoodQuantity Quantity,
    NutritionFacts Nutrition,
    DateOnly? ExpirationDate = null,
    string? Note = null,
    Guid? CatalogProductId = null);