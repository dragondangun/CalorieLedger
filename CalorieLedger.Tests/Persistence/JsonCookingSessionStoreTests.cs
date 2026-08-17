using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonCookingSessionStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonCookingSessionStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );

        filePath = Path.Combine(
            directoryPath,
            "cooking-sessions.json"
        );
    }

    [Fact]
    public void Save_Session_PersistsIngredientsAcrossStoreInstances() {
        var session = CreateSession();

        var firstStore = new JsonCookingSessionStore(filePath);

        firstStore.Save(session);

        var secondStore = new JsonCookingSessionStore(filePath);

        var saved = Assert.IsType<CookingSessionDraft>(secondStore.Get(session.Id));

        Assert.Equal(
            session.Id,
            saved.Id
        );

        Assert.Equal(
            session.Name,
            saved.Name
        );

        Assert.Equal(
            session.OutputWeightG,
            saved.OutputWeightG
        );

        Assert.Equal(
            session.Note,
            saved.Note
        );

        Assert.Equal(
            session.Ingredients.Count,
            saved.Ingredients.Count
        );

        for(var index = 0; index < session.Ingredients.Count; index++) {
            Assert.Equal(
                session.Ingredients[index],
                saved.Ingredients[index]
            );
        }
    }

    [Fact]
    public void Save_ExistingId_UpdatesPersistedSession() {
        var session = CreateSession();

        var store = new JsonCookingSessionStore(filePath);

        store.Save(session);

        store.Save(
            session with {
                Name = "Обновлённое блюдо",
                OutputWeightG = 450m,
            }
        );

        var reopenedStore = new JsonCookingSessionStore(filePath);

        var saved = Assert.IsType<CookingSessionDraft>(reopenedStore.Get(session.Id));

        Assert.Equal(
            "Обновлённое блюдо",
            saved.Name
        );

        Assert.Equal(
            450m,
            saved.OutputWeightG
        );

        Assert.Single(reopenedStore.GetAll());
    }

    [Fact]
    public void Delete_Session_PersistsDeletion() {
        var session = CreateSession();

        var store = new JsonCookingSessionStore(filePath);

        store.Save(session);

        Assert.True(store.Delete(session.Id));

        var reopenedStore = new JsonCookingSessionStore(filePath);

        Assert.Null(reopenedStore.Get(session.Id));
    }

    [Fact]
    public void GetAll_CorruptedJson_PreservesFileAndReturnsEmpty() {
        Directory.CreateDirectory(directoryPath);

        File.WriteAllText(
            filePath,
            "{ invalid json"
        );

        var store = new JsonCookingSessionStore(filePath);

        Assert.Empty(store.GetAll());

        Assert.False(File.Exists(filePath));

        Assert.Single(
            Directory.GetFiles(
                directoryPath,
                "cooking-sessions.json.corrupt-*"
            )
        );
    }

    private static CookingSessionDraft CreateSession() {
        return new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Курица",
            Ingredients: [
                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Куриная грудка",
                    Quantity: FoodQuantity.Grams(500m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 100m,
                        ProteinG: 20m,
                        FatG: 2m,
                        CarbsG: 0m
                    )
                ),
            ],
            OutputWeightG: 400m
        );
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
