using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Domain.Meals;

// конкретная съеденная позиция
public sealed record FoodLogEntry(
    Guid Id,
    Guid MealEntryId, // к какому приёму пищи относится запись
    string Name, // что съели
    FoodQuantity Quantity, // сколько съели
    NutritionFacts Nutrition, // КБЖУ
    FoodLogSource Source, // откуда запись
    Guid? SourceId = null, // id продукта/холодильника/блюда
    bool IsApproximate = false, // запись приблизительная
    string? Note = null); // комментарий