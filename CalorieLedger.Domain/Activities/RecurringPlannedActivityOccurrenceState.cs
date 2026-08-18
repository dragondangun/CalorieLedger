namespace CalorieLedger.Domain.Activities;

public sealed record RecurringPlannedActivityOccurrenceState(
    Guid ScheduleId,
    DateOnly Date,
    RecurringPlannedActivityOccurrenceStatus Status,
    Guid? ActivityId = null
);
