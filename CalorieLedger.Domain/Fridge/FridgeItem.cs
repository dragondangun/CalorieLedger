using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Fridge;

// конкретный остаток продукта в холодильнике
public sealed record FridgeItem(
    Guid Id,                         // id конкретного остатка в холодильнике
    string Name,                     // название
    FoodQuantity Quantity,           // сколько осталось
    NutritionFacts Nutrition,        // КБЖУ
    DateOnly? ExpirationDate = null, // срок годности
    string? Note = null,             // комментарий
    Guid? CatalogProductId = null);  // ссылка на базу продуктов