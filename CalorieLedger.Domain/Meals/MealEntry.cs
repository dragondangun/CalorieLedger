namespace CalorieLedger.Domain.Meals;

// конкретный приём пищи в конкретный день
public sealed record MealEntry(
    Guid Id,
    DateOnly Date,
    string Name,
    MealGroupRole Role,
    TimeOnly? EatenAt = null,
    string? Note = null);