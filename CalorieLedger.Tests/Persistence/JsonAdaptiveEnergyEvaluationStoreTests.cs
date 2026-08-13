using CalorieLedger.Domain.Adaptive;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonAdaptiveEnergyEvaluationStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonAdaptiveEnergyEvaluationStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );

        filePath = Path.Combine(
            directoryPath,
            "adaptive-energy-evaluations.json"
        );
    }

    [Fact]
    public void GetAll_MissingFile_ReturnsEmptyList() {
        var store = new JsonAdaptiveEnergyEvaluationStore(filePath);

        var entries = store.GetAll();

        Assert.Empty(
            entries
        );
    }

    [Fact]
    public void Save_NewEntry_PersistsBetweenStoreInstances() {
        var entry = CreateEntry(
            new DateOnly(2026, 8, 1),
            AdaptiveEnergyDeviationDirection.DecreaseCalories
        );

        var firstStore = new JsonAdaptiveEnergyEvaluationStore(filePath);

        firstStore.Save(entry);

        var secondStore = new JsonAdaptiveEnergyEvaluationStore(filePath);

        var savedEntry = Assert.Single(secondStore.GetAll());

        Assert.Equal(
            entry,
            savedEntry
        );
    }

    [Fact]
    public void Save_ExistingDate_ReplacesEntry() {
        var date = new DateOnly(2026, 8, 1);

        var store = new JsonAdaptiveEnergyEvaluationStore(filePath);

        store.Save(
            CreateEntry(
                date,
                AdaptiveEnergyDeviationDirection.DecreaseCalories
            )
        );

        store.Save(
            CreateEntry(
                date,
                AdaptiveEnergyDeviationDirection.IncreaseCalories
            )
        );

        var savedEntry = Assert.Single(store.GetAll());

        Assert.Equal(
            AdaptiveEnergyDeviationDirection.IncreaseCalories,
            savedEntry.DeviationDirection
        );
    }

    [Fact]
    public void GetAll_UnorderedEntries_ReturnsChronologicalHistory() {
        var store = new JsonAdaptiveEnergyEvaluationStore(filePath);

        var laterEntry = CreateEntry(
            new DateOnly(2026, 8, 8),
            AdaptiveEnergyDeviationDirection.DecreaseCalories
        );

        var earlierEntry = CreateEntry(
            new DateOnly(2026, 8, 1),
            AdaptiveEnergyDeviationDirection.DecreaseCalories
        );

        store.Save(laterEntry);

        store.Save(earlierEntry);

        var entries = store.GetAll();

        Assert.Equal(
            earlierEntry,
            entries[0]
        );

        Assert.Equal(
            laterEntry,
            entries[1]
        );
    }

    [Fact]
    public void Clear_RemovesPersistedHistory() {
        var firstStore = new JsonAdaptiveEnergyEvaluationStore(filePath);

        firstStore.Save(
            CreateEntry(
                new DateOnly(2026, 8, 1),
                AdaptiveEnergyDeviationDirection.DecreaseCalories
            )
        );

        firstStore.Clear();

        var secondStore = new JsonAdaptiveEnergyEvaluationStore(filePath);

        Assert.Empty(
            secondStore.GetAll()
        );
    }

    [Fact]
    public void GetAll_CorruptedJson_PreservesFileAndReturnsEmpty() {
        Directory.CreateDirectory(directoryPath);

        File.WriteAllText(
            filePath,
            "{ invalid json"
        );

        var store = new JsonAdaptiveEnergyEvaluationStore(filePath);

        var entries = store.GetAll();

        Assert.Empty(entries);

        Assert.False(File.Exists(filePath));

        var preservedFiles = Directory.GetFiles(
            directoryPath,
            "adaptive-energy-evaluations.json.corrupt-*"
        );

        Assert.Single(preservedFiles);
    }

    private static AdaptiveEnergyEvaluationEntry CreateEntry(
        DateOnly date,
        AdaptiveEnergyDeviationDirection direction
    ) {
        var adjustment = direction switch {
            AdaptiveEnergyDeviationDirection.IncreaseCalories => 100m,
            AdaptiveEnergyDeviationDirection.DecreaseCalories => -100m,
            _ => 0m
        };

        return new AdaptiveEnergyEvaluationEntry(
            EvaluationDate: date,
            AdjustmentStatus: AdaptiveEnergyAdjustmentStatus.RecommendationAvailable,
            DeviationDirection: direction,
            ObservedWeeklyWeightChangeKg: -0.2m,
            TargetWeeklyWeightChangeKg: -0.5m,
            RecommendedDailyAdjustmentKcal: adjustment,
            RecommendedTargetCaloriesKcal: 2000m + adjustment
        );
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
