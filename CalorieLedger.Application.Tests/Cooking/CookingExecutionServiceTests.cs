using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.Cooking;

public sealed class CookingExecutionServiceTests {
    [Fact]
    public void Execute_FridgeIngredient_ConsumesStockAndCreatesOutput() {
        var date = new DateOnly(2026, 8, 18);

        var fridgeStore = new InMemoryFridgeStore();
        var cookingStore = new InMemoryCookingSessionStore();
        var batchStore = new InMemoryCookingBatchStore();

        var fridgeItem = CreateFridgeItem(500m);

        fridgeStore.Save(fridgeItem);

        var session = CreateSession(
            fridgeItem.Id,
            ingredientQuantityG: 200m
        );

        cookingStore.Save(session);

        var service = new CookingExecutionService(
            cookingStore,
            batchStore,
            fridgeStore
        );

        var result = service.Execute(session.Id, date);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            300m,
            fridgeStore.Get(fridgeItem.Id)?.Quantity.Value
        );

        var output = Assert.Single(
            fridgeStore.GetAll(),
            item => item.Source == FridgeItemSource.CookingSession && item.SourceId == session.Id
        );

        Assert.Equal(
            400m,
            output.Quantity.Value
        );

        Assert.Equal(
            50m,
            output.Nutrition.CaloriesKcal
        );

        var batch = Assert.IsType<CookingBatch>(
            batchStore.GetBySessionId(session.Id)
        );

        Assert.Equal(
            date,
            batch.CookedDate
        );

        Assert.Equal(
            output.Id,
            batch.OutputFridgeItemId
        );
    }

    [Fact]
    public void Execute_SameSessionTwice_RejectsSecondExecutionWithoutChangingStock() {
        var date = new DateOnly(2026, 8, 18);

        var fridgeStore = new InMemoryFridgeStore();
        var cookingStore = new InMemoryCookingSessionStore();
        var batchStore = new InMemoryCookingBatchStore();

        var fridgeItem = CreateFridgeItem(500m);

        fridgeStore.Save(fridgeItem);

        var session = CreateSession(
            fridgeItem.Id,
            ingredientQuantityG: 200m
        );

        cookingStore.Save(session);

        var service = new CookingExecutionService(
            cookingStore,
            batchStore,
            fridgeStore
        );

        Assert.True(service.Execute(session.Id, date).IsSuccess);

        var secondResult = service.Execute(session.Id, date);

        Assert.False(secondResult.IsSuccess);

        Assert.Contains(
            CookingExecutionError.AlreadyCompleted,
            secondResult.Errors
        );

        Assert.Equal(
            300m,
            fridgeStore.Get(fridgeItem.Id)?.Quantity.Value
        );

        Assert.Equal(
            2,
            fridgeStore.GetAll().Count
        );

        Assert.Single(batchStore.GetAll());
    }

    [Fact]
    public void Execute_InsufficientFridgeStock_DoesNotCreateBatchOrOutput() {
        var date = new DateOnly(2026, 8, 18);

        var fridgeStore = new InMemoryFridgeStore();
        var cookingStore = new InMemoryCookingSessionStore();
        var batchStore = new InMemoryCookingBatchStore();

        var fridgeItem = CreateFridgeItem(100m);

        fridgeStore.Save(fridgeItem);

        var session = CreateSession(
            fridgeItem.Id,
            ingredientQuantityG: 200m
        );

        cookingStore.Save(session);

        var service = new CookingExecutionService(
            cookingStore,
            batchStore,
            fridgeStore
        );

        var result = service.Execute(session.Id, date);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            CookingExecutionError.InsufficientFridgeQuantity,
            result.Errors
        );

        Assert.Equal(
            100m,
            fridgeStore.Get(fridgeItem.Id)?.Quantity.Value
        );

        Assert.Single(fridgeStore.GetAll());

        Assert.Empty(batchStore.GetAll());
    }

    [Fact]
    public void Execute_DuplicateReferencesToSameFridgeItem_UsesCombinedQuantity() {
        var date = new DateOnly(2026, 8, 18);

        var fridgeStore = new InMemoryFridgeStore();
        var cookingStore = new InMemoryCookingSessionStore();
        var batchStore = new InMemoryCookingBatchStore();

        var fridgeItem = CreateFridgeItem(300m);

        fridgeStore.Save(fridgeItem);

        var session = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Тест",
            Ingredients: [
                CreateFridgeIngredient(fridgeItem.Id, 100m),
                CreateFridgeIngredient(fridgeItem.Id, 150m),
            ],
            OutputWeightG: 200m
        );

        cookingStore.Save(session);

        var service = new CookingExecutionService(
            cookingStore,
            batchStore,
            fridgeStore
        );

        var result = service.Execute(session.Id, date);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            50m,
            fridgeStore.Get(fridgeItem.Id)?.Quantity.Value
        );
    }

    private static FridgeItem CreateFridgeItem(decimal quantityG) {
        return new FridgeItem(
            Id: Guid.NewGuid(),
            Name: "Курица",
            Quantity: FoodQuantity.Grams(quantityG),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 100m,
                ProteinG: 20m,
                FatG: 2m,
                CarbsG: 0m
            )
        );
    }

    private static CookingSessionDraft CreateSession(
        Guid fridgeItemId,
        decimal ingredientQuantityG
    ) {
        return new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Готовая курица",
            Ingredients: [
                CreateFridgeIngredient(
                    fridgeItemId,
                    ingredientQuantityG
                ),
            ],
            OutputWeightG: 400m
        );
    }

    private static CookingIngredient CreateFridgeIngredient(
        Guid fridgeItemId,
        decimal quantityG
    ) {
        return new CookingIngredient(
            Id: Guid.NewGuid(),
            Name: "Курица",
            Quantity: FoodQuantity.Grams(quantityG),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 100m,
                ProteinG: 20m,
                FatG: 2m,
                CarbsG: 0m
            ),
            Source: CookingIngredientSource.FridgeItem,
            SourceId: fridgeItemId
        );
    }
}
