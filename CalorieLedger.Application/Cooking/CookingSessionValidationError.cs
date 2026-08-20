namespace CalorieLedger.Application.Cooking;

public enum CookingSessionValidationError {
    MissingId,
    MissingName,
    NoIngredients,
    InvalidOutputWeight,
    InvalidIngredientId,
    MissingIngredientName,
    InvalidIngredientQuantity,
    IncompatibleIngredientNutritionBasis,
    InvalidIngredientNutrition,
    InvalidNutritionOverride
}
