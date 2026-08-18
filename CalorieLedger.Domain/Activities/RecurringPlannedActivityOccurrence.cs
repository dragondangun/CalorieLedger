namespace CalorieLedger.Domain.Activities;

public sealed record RecurringPlannedActivityOccurrence(
    Guid ScheduleId,
    DateOnly Date,
    string Name,
    TimeOnly? PlannedAt = null,
    TimeSpan? Duration = null,
    string? PresetCode = null,
    decimal? MetValue = null,
    decimal? ManualBurnedCaloriesKcal = null,
    string? Note = null
);
