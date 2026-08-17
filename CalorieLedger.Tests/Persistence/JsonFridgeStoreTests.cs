using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonFridgeStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonFridgeStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );

        filePath = Path.Combine(
            directoryPath,
            "fridge.json"
        );
    }

    [Fact]
    public void Save_Item_PersistsQuantityAndSourceAcrossStoreInstances() {
        var sourceId = Guid.NewGuid();

        var item = new FridgeItem(
            Id: Guid.NewGuid(),
            Name: "Запеканка",
            Quantity: FoodQuantity.Grams(650m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 150m,
                ProteinG: 10m,
                FatG: 5m,
                CarbsG: 15m
            ),
            ExpirationDate: new DateOnly(2026, 8, 20),
            Note: "В контейнере",
            Source: FridgeItemSource.CookingSession,
            SourceId: sourceId
        );

        var firstStore = new JsonFridgeStore(filePath);

        firstStore.Save(item);

        var secondStore = new JsonFridgeStore(filePath);

        Assert.Equal(
            item,
            secondStore.Get(item.Id)
        );
    }

    [Fact]
    public void Save_ExistingId_UpdatesRemainingQuantity() {
        var item = new FridgeItem(
            Id: Guid.NewGuid(),
            Name: "Творог",
            Quantity: FoodQuantity.Grams(500m),
            Nutrition: NutritionFacts.Empty(NutritionBasis.Per100Grams)
        );

        var store = new JsonFridgeStore(filePath);

        store.Save(item);

        store.Save(
            item with {
                Quantity = FoodQuantity.Grams(250m),
            }
        );

        var reopenedStore = new JsonFridgeStore(filePath);

        Assert.Equal(
            250m,
            reopenedStore.Get(item.Id)?.Quantity.Value
        );

        Assert.Single(reopenedStore.GetAll());
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(
                directoryPath,
                recursive: true
            );
        }
    }
}
