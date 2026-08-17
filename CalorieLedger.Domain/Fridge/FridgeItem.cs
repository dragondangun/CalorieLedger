using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Fridge;

// конкретный остаток продукта или блюда в холодильнике
public sealed record FridgeItem(
    Guid Id,
    string Name,
    FoodQuantity Quantity,
    NutritionFacts Nutrition,
    DateOnly? ExpirationDate = null,
    string? Note = null,
    FridgeItemSource Source = FridgeItemSource.Manual,
    Guid? SourceId = null
);
