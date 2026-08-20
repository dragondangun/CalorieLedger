namespace CalorieLedger.Application.Cooking;

public enum CookingNutritionLlmParseErrorCode {
    InvalidJson,
    UnsupportedProtocol,
    SessionMismatch,
    RequestMismatch,
    MissingNutrition,
    InvalidNutrition
}

public sealed record CookingNutritionLlmParseError(
    CookingNutritionLlmParseErrorCode Code,
    string Path
);
