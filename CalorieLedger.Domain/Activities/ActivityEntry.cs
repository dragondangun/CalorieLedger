namespace CalorieLedger.Domain.Activities;

public sealed record ActivityEntry(
    Guid Id,
    DateOnly Date,
    string Name,
    decimal BurnedCaloriesKcal,
    TimeOnly? StartedAt = null,
    TimeSpan? Duration = null,
    string? Note = null);