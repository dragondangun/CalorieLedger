namespace CalorieLedger.Application.Activities;

public sealed record ActivityPresetSaveResult(
    bool IsSuccess,
    IReadOnlyList<ActivityPresetValidationError> Errors
);
