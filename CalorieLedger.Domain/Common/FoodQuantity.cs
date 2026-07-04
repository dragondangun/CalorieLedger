namespace CalorieLedger.Domain.Common;

// количество + единица
public sealed record FoodQuantity(
    decimal Value,
    FoodUnit Unit) {
    public static FoodQuantity Grams(decimal value) {
        return new FoodQuantity(value, FoodUnit.Gram);
    }

    public static FoodQuantity Milliliters(decimal value) {
        return new FoodQuantity(value, FoodUnit.Milliliter);
    }

    public static FoodQuantity Pieces(decimal value) {
        return new FoodQuantity(value, FoodUnit.Piece);
    }

    public static FoodQuantity Portions(decimal value) {
        return new FoodQuantity(value, FoodUnit.Portion);
    }
}