namespace CalorieLedger.Application.Activities;

public sealed record PlannedActivityDraft(
    Guid Id,
    DateOnly Date,
    string Name,
    TimeOnly? PlannedAt = null,
    TimeSpan? Duration = null,
    string? PresetCode = null,
    decimal? MetValue = null,
    decimal? ManualBurnedCaloriesKcal = null,
    string? Note = null
);
