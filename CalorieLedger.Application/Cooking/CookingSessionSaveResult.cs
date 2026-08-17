using System.Collections.Generic;

namespace CalorieLedger.Application.Cooking;

public sealed record CookingSessionSaveResult(
    bool IsSuccess,
    IReadOnlyList<CookingSessionValidationError> Errors
);
