namespace CalorieLedger.Application.Cooking;

public enum CookingExecutionError {
    MissingSession,
    AlreadyCompleted,
    InvalidSession,
    MissingFridgeSource,
    MissingFridgeItem,
    IncompatibleFridgeQuantity,
    InsufficientFridgeQuantity
}
