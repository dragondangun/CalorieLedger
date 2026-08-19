namespace CalorieLedger.Application.MealPlanning;

public sealed record MealPlanParseResult(
    bool IsSuccess,
    MealPlan? Plan,
    IReadOnlyList<MealPlanParseError> Errors
);
