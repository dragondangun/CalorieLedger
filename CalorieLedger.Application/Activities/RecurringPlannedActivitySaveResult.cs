namespace CalorieLedger.Application.Activities;

public sealed record RecurringPlannedActivitySaveResult(
    bool IsSuccess,
    IReadOnlyList<RecurringPlannedActivityValidationError> Errors
);
