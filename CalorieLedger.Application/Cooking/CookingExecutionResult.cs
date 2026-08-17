using CalorieLedger.Domain.Cooking;
using System.Collections.Generic;

namespace CalorieLedger.Application.Cooking;

public sealed record CookingExecutionResult(
    bool IsSuccess,
    CookingBatch? Batch,
    IReadOnlyList<CookingExecutionError> Errors
);
