using CalorieLedger.Domain.Activities;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonActivityStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonActivityStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );

        filePath = Path.Combine(
            directoryPath,
            "activities.json"
        );
    }

    [Fact]
    public void Save_Activity_PersistsAcrossStoreInstances() {
        var entry = new ActivityEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 18),
            Name: "HEMA",
            BurnedCaloriesKcal: 350m,
            StartedAt: new TimeOnly(18, 30),
            Duration: TimeSpan.FromMinutes(75),
            Note: "Тренировка"
        );

        var firstStore = new JsonActivityStore(filePath);

        firstStore.Save(entry);

        var secondStore = new JsonActivityStore(filePath);

        Assert.Equal(
            entry,
            secondStore.Get(
                entry.Id
            )
        );
    }

    [Fact]
    public void Save_ExistingId_UpdatesPersistedActivity() {
        var entry = new ActivityEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 18),
            Name: "Ходьба",
            BurnedCaloriesKcal: 150m
        );

        var store = new JsonActivityStore(filePath);

        store.Save(entry);

        store.Save(
            entry with {
                BurnedCaloriesKcal = 220m,
            }
        );

        var reopenedStore = new JsonActivityStore(filePath);

        Assert.Equal(
            220m,
            reopenedStore.Get(entry.Id)?.BurnedCaloriesKcal
        );

        Assert.Single(
            reopenedStore.Get(
                entry.Date,
                entry.Date
            )
        );
    }

    [Fact]
    public void Delete_Activity_PersistsDeletion() {
        var entry = new ActivityEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 18),
            Name: "Ходьба",
            BurnedCaloriesKcal: 150m
        );

        var store = new JsonActivityStore(filePath);

        store.Save(entry);

        Assert.True(store.Delete(entry.Id));

        var reopenedStore = new JsonActivityStore(filePath);

        Assert.Null(reopenedStore.Get(entry.Id));
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
