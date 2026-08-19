namespace CalorieLedger.Application.MealPlanning;

public enum MealPlanParseErrorCode {
    InvalidJson,
    UnsupportedProtocol,
    MissingDays,
    MissingDate,
    DuplicateDate,
    MissingMeals,
    MissingMealName,
    UnsupportedMealRole,
    MissingItems,
    MissingItemName,
    InvalidQuantity,
    UnsupportedQuantityUnit,
    InvalidNutrition
}

public sealed record MealPlanParseError(
    MealPlanParseErrorCode Code,
    string Path
);
