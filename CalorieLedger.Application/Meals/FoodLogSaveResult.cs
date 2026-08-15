using System.Collections.Generic;

namespace CalorieLedger.Application.Meals;

public sealed record FoodLogSaveResult(
    bool IsSuccess,
    IReadOnlyList<FoodLogValidationError> Errors
);
