using System;

namespace CalorieLedger.Application.Activities;

public sealed record ActivityDraft(
    Guid Id,
    DateOnly Date,
    string Name,
    decimal? BurnedCaloriesKcal,
    TimeOnly? StartedAt = null,
    TimeSpan? Duration = null,
    string? Note = null
);
