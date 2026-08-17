using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonCookingBatchStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonCookingBatchStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );

        filePath = Path.Combine(directoryPath, "cooking-batches.json");
    }

    [Fact]
    public void Save_Batch_PersistsSnapshotAcrossStoreInstances() {
        var batch = CreateBatch();

        var firstStore = new JsonCookingBatchStore(filePath);

        firstStore.Save(batch);

        var secondStore = new JsonCookingBatchStore(filePath);

        var saved = Assert.IsType<CookingBatch>(secondStore.GetBySessionId(batch.SessionId));

        Assert.Equal(
            batch.Id,
            saved.Id
        );

        Assert.Equal(
            batch.SessionId,
            saved.SessionId
        );

        Assert.Equal(
            batch.Name,
            saved.Name
        );

        Assert.Equal(
            batch.OutputWeightG,
            saved.OutputWeightG
        );

        Assert.Equal(
            batch.CookedDate,
            saved.CookedDate
        );

        Assert.Equal(
            batch.OutputFridgeItemId,
            saved.OutputFridgeItemId
        );

        Assert.Equal(
            batch.Nutrition,
            saved.Nutrition
        );

        Assert.Equal(
            batch.Ingredients.Count,
            saved.Ingredients.Count
        );

        for(var index = 0; index < batch.Ingredients.Count; index++) {
            Assert.Equal(
                batch.Ingredients[index],
                saved.Ingredients[index]
            );
        }
    }

    private static CookingBatch CreateBatch() {
        var ingredient = new CookingIngredient(
            Id: Guid.NewGuid(),
            Name: "Курица",
            Quantity: FoodQuantity.Grams(200m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 100m,
                ProteinG: 20m,
                FatG: 2m,
                CarbsG: 0m
            )
        );

        return new CookingBatch(
            Id: Guid.NewGuid(),
            SessionId: Guid.NewGuid(),
            Name: "Готовая курица",
            Ingredients: [ingredient],
            OutputWeightG: 400m,
            Nutrition: CookingNutritionCalculator.Calculate(
                new CookingSessionDraft(
                    Id: Guid.NewGuid(),
                    Name: "Готовая курица",
                    Ingredients: [ingredient],
                    OutputWeightG: 400m
                )
            ),
            CookedDate: new DateOnly(2026, 8, 18),
            OutputFridgeItemId: Guid.NewGuid()
        );
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
