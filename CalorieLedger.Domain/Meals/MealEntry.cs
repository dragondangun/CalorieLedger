namespace CalorieLedger.Domain.Meals;

public sealed record MealEntry(
    Guid Id,
    DateOnly Date,
    string Name,
    MealGroupRole Role,
    TimeOnly? EatenAt = null,
    string? Note = null);