namespace CalorieLedger.Application.Meals;

public enum FoodLogValidationError {
    MissingId,
    FutureDate,
    MissingName,
    InvalidQuantity,
    IncompatibleNutritionBasis,
    InvalidCalories,
    InvalidProtein,
    InvalidFat,
    InvalidCarbs
}
