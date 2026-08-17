using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;

namespace CalorieLedger.Application.Fridge;

public sealed class FridgeInventoryService {
    private readonly IFridgeStore fridgeStore;

    public FridgeInventoryService(IFridgeStore fridgeStore) {
        ArgumentNullException.ThrowIfNull(fridgeStore);

        this.fridgeStore = fridgeStore;
    }

    public IReadOnlyList<FridgeItem> Search(string? query) {
        var items = fridgeStore.GetAll();

        if(string.IsNullOrWhiteSpace(query)) {
            return items;
        }

        var normalizedQuery = query.Trim();

        return [
            .. items.Where(
                item => item.Name.Contains(
                    normalizedQuery,
                    StringComparison.OrdinalIgnoreCase
                )
            ),
        ];
    }

    public FridgeItem? Get(Guid id) {
        return fridgeStore.Get(id);
    }

    public FridgeItemAddResult AddCatalogProduct(
        ProductCatalogItem product,
        decimal quantityValue,
        DateOnly? expirationDate = null,
        string? note = null
    ) {
        ArgumentNullException.ThrowIfNull(product);

        if(quantityValue <= 0m) {
            return Failure(FridgeItemValidationError.InvalidQuantity);
        }

        var unit = GetCompatibleUnit(product.Nutrition.Basis);

        if(unit is null) {
            return Failure(FridgeItemValidationError.UnsupportedNutritionBasis);
        }

        var item = new FridgeItem(
            Id: Guid.NewGuid(),
            Name: product.Name,
            Quantity: new FoodQuantity(
                quantityValue,
                unit.Value
            ),
            Nutrition: product.Nutrition,
            ExpirationDate: expirationDate,
            Note: NormalizeOptionalText(note),
            Source: FridgeItemSource.CatalogProduct,
            SourceId: product.Id
        );

        fridgeStore.Save(item);

        return Success(item);
    }

    public FridgeItemAddResult AddCookingSession(
        CookingSessionDraft session,
        CookingNutritionResult nutrition,
        DateOnly? expirationDate = null,
        string? note = null
    ) {
        ArgumentNullException.ThrowIfNull(session);

        ArgumentNullException.ThrowIfNull(nutrition);

        if(session.OutputWeightG <= 0m) {
            return Failure(FridgeItemValidationError.InvalidQuantity);
        }

        var item = new FridgeItem(
            Id: Guid.NewGuid(),
            Name: session.Name,
            Quantity: FoodQuantity.Grams(session.OutputWeightG),
            Nutrition: nutrition.NutritionPer100Grams,
            ExpirationDate: expirationDate,
            Note: NormalizeOptionalText(note),
            Source: FridgeItemSource.CookingSession,
            SourceId: session.Id
        );

        fridgeStore.Save(item);

        return Success(item);
    }

    public FoodLogDraft? CreateFoodLogDraft(
        Guid fridgeItemId,
        DateOnly date
    ) {
        var item = fridgeStore.Get(fridgeItemId);

        if(item is null || item.Quantity.Value <= 0m) {
            return null;
        }

        var defaultQuantity = GetDefaultFoodQuantity(item.Quantity);

        return new FoodLogDraft(
            Id: Guid.NewGuid(),
            Date: date,
            Name: item.Name,
            MealRole: item.Source == FridgeItemSource.CookingSession
                ? MealGroupRole.Custom
                : MealGroupRole.Snack,
            QuantityValue: defaultQuantity,
            QuantityUnit: item.Quantity.Unit,
            NutritionBasis: item.Nutrition.Basis,
            CaloriesKcal: item.Nutrition.CaloriesKcal,
            ProteinG: item.Nutrition.ProteinG,
            FatG: item.Nutrition.FatG,
            CarbsG: item.Nutrition.CarbsG,
            Source: FoodLogSource.FridgeItem,
            SourceId: item.Id
        );
    }

    public bool Delete(Guid id) {
        return fridgeStore.Delete(id);
    }

    private static decimal GetDefaultFoodQuantity(FoodQuantity availableQuantity) {
        var preferredQuantity = availableQuantity.Unit switch {
            FoodUnit.Gram => 100m,
            FoodUnit.Milliliter => 100m,
            FoodUnit.Piece => 1m,
            FoodUnit.Portion => 1m,
            _ => throw new ArgumentOutOfRangeException(
                nameof(availableQuantity),
                availableQuantity.Unit,
                null
            )
        };

        return Math.Min(
            preferredQuantity,
            availableQuantity.Value
        );
    }

    private static FoodUnit? GetCompatibleUnit(NutritionBasis basis) {
        return basis switch {
            NutritionBasis.Per100Grams => FoodUnit.Gram,
            NutritionBasis.Per100Milliliters => FoodUnit.Milliliter,
            NutritionBasis.PerItem => FoodUnit.Piece,
            _ => null
        };
    }

    private static FridgeItemAddResult Success(FridgeItem item) {
        return new FridgeItemAddResult(
            IsSuccess: true,
            Item: item,
            Errors: []
        );
    }

    private static FridgeItemAddResult Failure(FridgeItemValidationError error) {
        return new FridgeItemAddResult(
            IsSuccess: false,
            Item: null,
            Errors: [error]
        );
    }

    private static string? NormalizeOptionalText(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
