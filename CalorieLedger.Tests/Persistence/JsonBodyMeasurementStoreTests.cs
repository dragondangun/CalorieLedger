using CalorieLedger.Domain.Profile;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonBodyMeasurementStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonBodyMeasurementStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N"));

        filePath = Path.Combine(
            directoryPath,
            "body-measurements.json");
    }

    [Fact]
    public void GetAll_MissingFile_ReturnsEmptyList() {
        var store = new JsonBodyMeasurementStore(filePath);

        var entries = store.GetAll();

        Assert.Empty(entries);
    }

    [Fact]
    public void Save_NewEntry_PersistsIt() {
        var entry = CreateEntry(weightKg: 80m);

        var firstStore = new JsonBodyMeasurementStore(filePath);

        firstStore.Save(entry);

        var secondStore = new JsonBodyMeasurementStore(filePath);

        var savedEntry = Assert.Single(secondStore.GetAll());

        Assert.Equal(
            entry,
            savedEntry);
    }

    [Fact]
    public void Save_ExistingId_UpdatesEntry() {
        var id = Guid.NewGuid();

        var store = new JsonBodyMeasurementStore(filePath);

        store.Save(
            CreateEntry(
                id: id,
                weightKg: 80m));

        store.Save(
            CreateEntry(
                id: id,
                weightKg: 79.5m));

        var savedEntry = Assert.Single(store.GetAll());

        Assert.Equal(
            79.5m,
            savedEntry.WeightKg);
    }

    [Fact]
    public void Delete_ExistingEntry_RemovesItFromFile() {
        var entry = CreateEntry(weightKg: 80m);

        var store = new JsonBodyMeasurementStore(filePath);

        store.Save(entry);

        var deleted = store.Delete(entry.Id);

        Assert.True(deleted);

        var reopenedStore = new JsonBodyMeasurementStore(filePath);

        Assert.Empty(reopenedStore.GetAll());
    }

    [Fact]
    public void Delete_UnknownId_ReturnsFalse() {
        var store = new JsonBodyMeasurementStore(filePath);

        var deleted = store.Delete(Guid.NewGuid());

        Assert.False(deleted);
    }

    [Fact]
    public void GetAll_SortsEntriesByDate() {
        var store = new JsonBodyMeasurementStore(filePath);

        var laterEntry = CreateEntry(
            date: new DateOnly(
                2026,
                7,
                20),
            weightKg: 79m);

        var earlierEntry = CreateEntry(
            date: new DateOnly(
                2026,
                7,
                18),
            weightKg: 80m);

        store.Save(laterEntry);
        store.Save(earlierEntry);

        var entries = store.GetAll();

        Assert.Equal(
            earlierEntry.Id,
            entries[0].Id);

        Assert.Equal(
            laterEntry.Id,
            entries[1].Id);
    }

    [Fact]
    public void GetAll_CorruptedJson_PreservesFileAndReturnsEmpty() {
        Directory.CreateDirectory(directoryPath);

        File.WriteAllText(
            filePath,
            "{ invalid json");

        var store = new JsonBodyMeasurementStore(filePath);

        var entries = store.GetAll();

        Assert.Empty(entries);
        Assert.False(File.Exists(filePath));

        var preservedFiles = Directory.GetFiles(
            directoryPath,
            "body-measurements.json.corrupt-*");

        Assert.Single(preservedFiles);
    }

    private static BodyMeasurementEntry CreateEntry(
            Guid? id = null,
            DateOnly? date = null,
            decimal weightKg = 80m) {
        return new BodyMeasurementEntry(
            Id: id ?? Guid.NewGuid(),
            Date: date ?? new DateOnly(
                2026,
                7,
                20),
            WeightKg: weightKg,
            BodyFatPercent: 20m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35m,
            MusclePercent: 35m / weightKg * 100m);
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(
                directoryPath,
                recursive: true);
        }
    }
}