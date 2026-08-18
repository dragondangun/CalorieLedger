namespace CalorieLedger.Application.Activities;

public sealed record PlannedActivitySaveResult(
    bool IsSuccess,
    IReadOnlyList<PlannedActivityValidationError> Errors
);
