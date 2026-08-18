namespace CalorieLedger.Application.Activities;

public sealed record RecurringPlannedActivityDraft(
    Guid Id,
    DateOnly StartDate,
    DayOfWeek DayOfWeek,
    int IntervalWeeks,
    string Name,
    TimeOnly? PlannedAt = null,
    TimeSpan? Duration = null,
    string? PresetCode = null,
    decimal? MetValue = null,
    decimal? ManualBurnedCaloriesKcal = null,
    string? Note = null
);
