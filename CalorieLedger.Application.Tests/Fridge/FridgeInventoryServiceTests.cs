using CalorieLedger.Application.Fridge;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;

namespace CalorieLedger.Application.Tests.Fridge;

public sealed class FridgeInventoryServiceTests {
    [Fact]
    public void AddCatalogProduct_PersistsCatalogSourceAndCompatibleQuantity() {
        var store = new InMemoryFridgeStore();

        var service = new FridgeInventoryService(store);

        var product = new ProductCatalogItem(
            Id: Guid.NewGuid(),
            Name: "Творог",
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 120m,
                ProteinG: 17m,
                FatG: 5m,
                CarbsG: 3m
            )
        );

        var result = service.AddCatalogProduct(
            product,
            500m
        );

        Assert.True(result.IsSuccess);

        var item = Assert.IsType<FridgeItem>(result.Item);

        Assert.Equal(
            500m,
            item.Quantity.Value
        );

        Assert.Equal(
            FridgeItemSource.CatalogProduct,
            item.Source
        );

        Assert.Equal(
            product.Id,
            item.SourceId
        );
    }

    [Fact]
    public void AddCookingSession_UsesOutputWeightAndPer100GramNutrition() {
        var store = new InMemoryFridgeStore();

        var service = new FridgeInventoryService(store);

        var session = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Запеканка",
            Ingredients: [],
            OutputWeightG: 800m
        );

        var per100 = new NutritionFacts(
            Basis: NutritionBasis.Per100Grams,
            CaloriesKcal: 150m,
            ProteinG: 10m,
            FatG: 5m,
            CarbsG: 15m
        );

        var result = service.AddCookingSession(
            session,
            new CookingNutritionResult(
                TotalNutrition: new NutritionTotals(
                    CaloriesKcal: 1200m,
                    ProteinG: 80m,
                    FatG: 40m,
                    CarbsG: 120m
                ),
                NutritionPer100Grams: per100
            )
        );

        var item = Assert.IsType<FridgeItem>(result.Item);

        Assert.Equal(
            800m,
            item.Quantity.Value
        );

        Assert.Equal(
            per100,
            item.Nutrition
        );

        Assert.Equal(
            FridgeItemSource.CookingSession,
            item.Source
        );

        Assert.Equal(
            session.Id,
            item.SourceId
        );
    }

    [Fact]
    public void CreateFoodLogDraft_AvailableLessThanDefault_UsesRemainingQuantity() {
        var date = new DateOnly(2026, 8, 17);

        var store = new InMemoryFridgeStore();

        var item = new FridgeItem(
            Id: Guid.NewGuid(),
            Name: "Сыр",
            Quantity: FoodQuantity.Grams(65m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 350m,
                ProteinG: 25m,
                FatG: 28m,
                CarbsG: 1m
            )
        );

        store.Save(item);

        var service = new FridgeInventoryService(store);

        var draft = Assert.IsType<CalorieLedger.Application.Meals.FoodLogDraft>(
            service.CreateFoodLogDraft(
                item.Id,
                date
            )
        );

        Assert.Equal(
            65m,
            draft.QuantityValue
        );

        Assert.Equal(
            FoodLogSource.FridgeItem,
            draft.Source
        );

        Assert.Equal(
            item.Id,
            draft.SourceId
        );
    }
}
