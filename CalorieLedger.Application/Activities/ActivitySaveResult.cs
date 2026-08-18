using System.Collections.Generic;

namespace CalorieLedger.Application.Activities;

public sealed record ActivitySaveResult(
    bool IsSuccess,
    IReadOnlyList<ActivityValidationError> Errors
);
