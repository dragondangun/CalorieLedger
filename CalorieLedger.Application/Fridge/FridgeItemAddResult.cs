using CalorieLedger.Domain.Fridge;

namespace CalorieLedger.Application.Fridge;

public sealed record FridgeItemAddResult(
    bool IsSuccess,
    FridgeItem? Item,
    IReadOnlyList<FridgeItemValidationError> Errors
);
