using CalorieLedger.Application.Cooking;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;

namespace CalorieLedger.Application.Tests.Cooking;

public sealed class CookingSessionServiceTests {
    [Fact]
    public void Save_ValidSession_PersistsNormalizedDraft() {
        var store = new InMemoryCookingSessionStore();

        var service = new CookingSessionService(store);

        var draft = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "  Курица  ",
            Ingredients: [
                CreateIngredient(
                    grams: 500m,
                    caloriesPer100Grams: 100m
                ),
            ],
            OutputWeightG: 400m,
            Note: "  Тест  "
        );

        var result = service.Save(draft);

        Assert.True(result.IsSuccess);

        var saved = Assert.IsType<CookingSessionDraft>(store.Get(draft.Id));

        Assert.Equal(
            "Курица",
            saved.Name
        );

        Assert.Equal(
            "Тест",
            saved.Note
        );
    }

    [Fact]
    public void Save_InvalidSession_ReturnsRelevantErrors() {
        var service = new CookingSessionService(
            new InMemoryCookingSessionStore()
        );

        var draft = new CookingSessionDraft(
            Id: Guid.Empty,
            Name: " ",
            Ingredients: [],
            OutputWeightG: 0m
        );

        var result = service.Save(draft);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            CookingSessionValidationError.MissingId,
            result.Errors
        );

        Assert.Contains(
            CookingSessionValidationError.MissingName,
            result.Errors
        );

        Assert.Contains(
            CookingSessionValidationError.NoIngredients,
            result.Errors
        );

        Assert.Contains(
            CookingSessionValidationError.InvalidOutputWeight,
            result.Errors
        );
    }

    [Fact]
    public void CreateCatalogIngredient_UsesProductNutritionAndCompatibleUnit() {
        var service = new CookingSessionService(new InMemoryCookingSessionStore());

        var product = new ProductCatalogItem(
            Id: Guid.NewGuid(),
            Name: "Курица",
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 100m,
                ProteinG: 20m,
                FatG: 2m,
                CarbsG: 0m
            )
        );

        var ingredient = Assert.IsType<CookingIngredient>(service.CreateCatalogIngredient(product, 500m));

        Assert.Equal(
            500m,
            ingredient.Quantity.Value
        );

        Assert.Equal(
            FoodUnit.Gram,
            ingredient.Quantity.Unit
        );

        Assert.Equal(
            CookingIngredientSource.ProductCatalog,
            ingredient.Source
        );

        Assert.Equal(
            product.Id,
            ingredient.SourceId
        );

        Assert.Equal(
            product.Nutrition,
            ingredient.Nutrition
        );
    }

    [Fact]
    public void CreateFoodLogDraft_SavedCooking_UsesCalculatedPer100GramNutrition() {
        var date = new DateOnly(2026, 8, 17);

        var store = new InMemoryCookingSessionStore();

        var service = new CookingSessionService(store);

        var cooking = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Курица",
            Ingredients: [
                CreateIngredient(
                    grams: 500m,
                    caloriesPer100Grams: 100m
                ),
            ],
            OutputWeightG: 400m
        );

        Assert.True(service.Save(cooking).IsSuccess);

        var result = Assert.IsType<CalorieLedger.Application.Meals.FoodLogDraft>(
            service.CreateFoodLogDraft(cooking.Id, date)
        );

        Assert.Equal(
            date,
            result.Date
        );

        Assert.Equal(
            "Курица",
            result.Name
        );

        Assert.Equal(
            100m,
            result.QuantityValue
        );

        Assert.Equal(
            FoodUnit.Gram,
            result.QuantityUnit
        );

        Assert.Equal(
            NutritionBasis.Per100Grams,
            result.NutritionBasis
        );

        Assert.Equal(
            125m,
            result.CaloriesKcal
        );

        Assert.Equal(
            FoodLogSource.CookingSession,
            result.Source
        );

        Assert.Equal(
            cooking.Id,
            result.SourceId
        );
    }

    private static CookingIngredient CreateIngredient(
        decimal grams,
        decimal caloriesPer100Grams
    ) {
        return new CookingIngredient(
            Id: Guid.NewGuid(),
            Name: "Ингредиент",
            Quantity: FoodQuantity.Grams(grams),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: caloriesPer100Grams,
                ProteinG: 20m,
                FatG: 2m,
                CarbsG: 0m
            )
        );
    }
}
