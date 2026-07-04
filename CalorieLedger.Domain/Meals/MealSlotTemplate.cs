namespace CalorieLedger.Domain.Meals;

public sealed record MealSlotTemplate(
    Guid Id,
    string Name,
    MealGroupRole Role,
    int SortOrder,
    TimeOnly? DefaultTime = null);