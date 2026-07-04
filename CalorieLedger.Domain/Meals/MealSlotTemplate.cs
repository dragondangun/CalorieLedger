namespace CalorieLedger.Domain.Meals;

// шаблон приёмов пищи
public sealed record MealSlotTemplate(
    Guid Id,
    string Name,
    MealGroupRole Role,
    int SortOrder,
    TimeOnly? DefaultTime = null);