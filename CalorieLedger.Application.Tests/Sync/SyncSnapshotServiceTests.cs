using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Sync;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.Sync;

public sealed class SyncSnapshotServiceTests {
    [Fact]
    public void CreateExport_IncludesFridgeAndOnlyUnfinishedCookingSessions() {
        var fridgeStore = new InMemoryFridgeStore();
        var cookingSessionStore = new InMemoryCookingSessionStore();
        var cookingBatchStore = new InMemoryCookingBatchStore();
        var identity = new SyncDeviceIdentity(Guid.NewGuid());
        var time = new DateTimeOffset(2026, 8, 20, 10, 30, 0, TimeSpan.Zero);

        var fridgeItem = CreateFridgeItem("Кефир");
        var unfinished = CreateCookingSession("Суп");
        var completed = CreateCookingSession("Запеканка");

        fridgeStore.Save(fridgeItem);
        cookingSessionStore.Save(unfinished);
        cookingSessionStore.Save(completed);
        cookingBatchStore.Save(CreateBatch(completed));

        var service = new SyncSnapshotService(
            fridgeStore,
            cookingSessionStore,
            cookingBatchStore,
            new InMemorySyncDeviceIdentityStore(identity),
            new FixedTimeProvider(time)
        );

        var json = service.CreateExport();

        var remoteService = CreateService(
            deviceId: Guid.NewGuid()
        );
        var parsed = remoteService.Parse(json);

        Assert.True(parsed.IsSuccess);
        var snapshot = Assert.IsType<SyncSnapshot>(parsed.Snapshot);
        Assert.Equal(SyncSnapshotService.ProtocolName, snapshot.Protocol);
        Assert.Equal(identity.Id, snapshot.SourceDeviceId);
        Assert.Equal(time, snapshot.GeneratedAtUtc);
        Assert.Equal(fridgeItem, Assert.Single(snapshot.FridgeItems));
        AssertCookingSessionEqual(
            unfinished,
            Assert.Single(snapshot.CookingSessions)
        );
    }

    [Fact]
    public void Preview_RoundTrippedEquivalentCookingSession_IsUnchanged() {
        var localFridge = new InMemoryFridgeStore();
        var localCooking = new InMemoryCookingSessionStore();
        var localBatches = new InMemoryCookingBatchStore();
        var localDeviceId = Guid.NewGuid();
        var session = CreateCookingSession("Суп");

        localCooking.Save(session);

        var localService = new SyncSnapshotService(
            localFridge,
            localCooking,
            localBatches,
            new InMemorySyncDeviceIdentityStore(
                new SyncDeviceIdentity(localDeviceId)
            )
        );

        var remoteCooking = new InMemoryCookingSessionStore();
        remoteCooking.Save(session);

        var remoteService = new SyncSnapshotService(
            new InMemoryFridgeStore(),
            remoteCooking,
            new InMemoryCookingBatchStore(),
            new InMemorySyncDeviceIdentityStore(
                new SyncDeviceIdentity(Guid.NewGuid())
            )
        );

        var parsed = localService.Parse(remoteService.CreateExport());

        Assert.True(parsed.IsSuccess);
        var snapshot = Assert.IsType<SyncSnapshot>(parsed.Snapshot);
        var preview = localService.Preview(snapshot);

        Assert.Equal(0, preview.CookingSessionsAdded);
        Assert.Equal(0, preview.CookingSessionsUpdated);
        Assert.Equal(1, preview.CookingSessionsUnchanged);
    }

    [Fact]
    public void Parse_OwnDeviceSnapshot_IsRejected() {
        var deviceId = Guid.NewGuid();
        var service = CreateService(deviceId);
        var json = service.CreateExport();

        var result = service.Parse(json);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Snapshot);
        Assert.Contains(
            SyncSnapshotParseError.OwnDeviceSnapshot,
            result.Errors
        );
    }

    [Fact]
    public void PreviewAndApply_MergesIncomingItemsWithoutDeletingLocalOnlyItems() {
        var localFridge = new InMemoryFridgeStore();
        var localCooking = new InMemoryCookingSessionStore();
        var localBatches = new InMemoryCookingBatchStore();
        var localDeviceId = Guid.NewGuid();
        var service = new SyncSnapshotService(
            localFridge,
            localCooking,
            localBatches,
            new InMemorySyncDeviceIdentityStore(
                new SyncDeviceIdentity(localDeviceId)
            )
        );

        var sharedFridgeId = Guid.NewGuid();
        var localOnly = CreateFridgeItem("Локальный продукт");
        var oldShared = CreateFridgeItem("Йогурт") with {
            Id = sharedFridgeId,
            Quantity = FoodQuantity.Grams(100m),
        };
        var newShared = oldShared with {
            Quantity = FoodQuantity.Grams(250m),
        };
        var incomingOnly = CreateFridgeItem("Удалённый продукт");
        var incomingCooking = CreateCookingSession("Овощное рагу");

        localFridge.SaveMany([localOnly, oldShared]);

        var snapshot = new SyncSnapshot(
            Protocol: SyncSnapshotService.ProtocolName,
            SnapshotId: Guid.NewGuid(),
            SourceDeviceId: Guid.NewGuid(),
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            FridgeItems: [newShared, incomingOnly],
            CookingSessions: [incomingCooking]
        );

        var preview = service.Preview(snapshot);

        Assert.Equal(1, preview.FridgeAdded);
        Assert.Equal(1, preview.FridgeUpdated);
        Assert.Equal(1, preview.CookingSessionsAdded);

        var result = service.Apply(snapshot);

        Assert.Equal(1, result.FridgeAdded);
        Assert.Equal(1, result.FridgeUpdated);
        Assert.Equal(250m, localFridge.Get(sharedFridgeId)?.Quantity.Value);
        Assert.NotNull(localFridge.Get(incomingOnly.Id));
        Assert.NotNull(localFridge.Get(localOnly.Id));
        Assert.Equal(incomingCooking, localCooking.Get(incomingCooking.Id));
    }

    [Fact]
    public void Apply_DoesNotOverwriteCookingSessionAlreadyCompletedLocally() {
        var localFridge = new InMemoryFridgeStore();
        var localCooking = new InMemoryCookingSessionStore();
        var localBatches = new InMemoryCookingBatchStore();
        var localSession = CreateCookingSession("Каша");
        var remoteVersion = localSession with {
            Name = "Удалённое имя",
        };

        localCooking.Save(localSession);
        localBatches.Save(CreateBatch(localSession));

        var service = new SyncSnapshotService(
            localFridge,
            localCooking,
            localBatches,
            new InMemorySyncDeviceIdentityStore(
                new SyncDeviceIdentity(Guid.NewGuid())
            )
        );

        var snapshot = new SyncSnapshot(
            Protocol: SyncSnapshotService.ProtocolName,
            SnapshotId: Guid.NewGuid(),
            SourceDeviceId: Guid.NewGuid(),
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            FridgeItems: [],
            CookingSessions: [remoteVersion]
        );

        var preview = service.Preview(snapshot);
        var result = service.Apply(snapshot);

        Assert.Equal(1, preview.CompletedCookingSessionConflicts);
        Assert.Equal(1, result.CompletedCookingSessionConflicts);
        Assert.Equal(localSession, localCooking.Get(localSession.Id));
    }


    private static void AssertCookingSessionEqual(
        CookingSessionDraft expected,
        CookingSessionDraft actual
    ) {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.OutputWeightG, actual.OutputWeightG);
        Assert.Equal(expected.Note, actual.Note);
        Assert.Equal(
            expected.NutritionPer100GramsOverride,
            actual.NutritionPer100GramsOverride
        );
        Assert.Equal(expected.Ingredients.Count, actual.Ingredients.Count);

        for(var index = 0; index < expected.Ingredients.Count; index++) {
            Assert.Equal(
                expected.Ingredients[index],
                actual.Ingredients[index]
            );
        }
    }

    private static SyncSnapshotService CreateService(Guid deviceId) {
        return new SyncSnapshotService(
            new InMemoryFridgeStore(),
            new InMemoryCookingSessionStore(),
            new InMemoryCookingBatchStore(),
            new InMemorySyncDeviceIdentityStore(
                new SyncDeviceIdentity(deviceId)
            )
        );
    }

    private static FridgeItem CreateFridgeItem(string name) {
        return new FridgeItem(
            Id: Guid.NewGuid(),
            Name: name,
            Quantity: FoodQuantity.Grams(500m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 100m,
                ProteinG: 5m,
                FatG: 3m,
                CarbsG: 10m
            )
        );
    }

    private static CookingSessionDraft CreateCookingSession(string name) {
        return new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: name,
            Ingredients: [
                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Ингредиент",
                    Quantity: FoodQuantity.Grams(100m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 100m,
                        ProteinG: 5m,
                        FatG: 3m,
                        CarbsG: 10m
                    )
                ),
            ],
            OutputWeightG: 100m
        );
    }

    private static CookingBatch CreateBatch(CookingSessionDraft session) {
        return new CookingBatch(
            Id: Guid.NewGuid(),
            SessionId: session.Id,
            Name: session.Name,
            Ingredients: session.Ingredients,
            OutputWeightG: session.OutputWeightG,
            Nutrition: new CookingNutritionResult(
                TotalNutrition: new NutritionTotals(
                    CaloriesKcal: 100m,
                    ProteinG: 5m,
                    FatG: 3m,
                    CarbsG: 10m
                ),
                NutritionPer100Grams: new NutritionFacts(
                    Basis: NutritionBasis.Per100Grams,
                    CaloriesKcal: 100m,
                    ProteinG: 5m,
                    FatG: 3m,
                    CarbsG: 10m
                )
            ),
            CookedDate: new DateOnly(2026, 8, 20),
            OutputFridgeItemId: Guid.NewGuid()
        );
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow):TimeProvider {
        public override DateTimeOffset GetUtcNow() {
            return utcNow;
        }
    }
}
