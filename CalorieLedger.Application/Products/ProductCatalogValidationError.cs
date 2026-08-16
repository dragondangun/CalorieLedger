namespace CalorieLedger.Application.Products;

public enum ProductCatalogValidationError {
    MissingId,
    MissingName,
    InvalidNutritionBasis,
    InvalidCalories,
    InvalidProtein,
    InvalidFat,
    InvalidCarbs
}
