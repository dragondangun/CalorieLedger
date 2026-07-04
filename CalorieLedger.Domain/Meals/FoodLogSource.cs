namespace CalorieLedger.Domain.Meals;

// откуда взялась съеденная еда
public enum FoodLogSource {
    Manual = 1,
    CatalogProduct = 2,
    FridgeItem = 3,
    CookingSession = 4,
    Recipe = 5,
    Approximation = 6
}