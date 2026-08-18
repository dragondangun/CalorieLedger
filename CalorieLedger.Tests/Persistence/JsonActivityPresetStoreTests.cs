using CalorieLedger.Application.Activities;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonActivityPresetStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonActivityPresetStoreTests() {
        directoryPath = Path.Combine(Path.GetTempPath(), "CalorieLedger.Tests", Guid.NewGuid().ToString("N"));
        filePath = Path.Combine(directoryPath, "activity-presets.json");
    }

    [Fact]
    public void Save_Preset_PersistsAcrossStoreInstances() {
        var preset = new ActivityPreset("custom:test", "HEMA", 7m);
        var firstStore = new JsonActivityPresetStore(filePath);

        firstStore.Save(preset);

        var secondStore = new JsonActivityPresetStore(filePath);

        Assert.Equal(preset, secondStore.Get(preset.Code));
    }

    [Fact]
    public void Save_ExistingCode_UpdatesPreset() {
        var store = new JsonActivityPresetStore(filePath);
        var preset = new ActivityPreset("custom:test", "HEMA", 6m);

        store.Save(preset);
        store.Save(preset with { Name = "HEMA, интенсивно", MetValue = 8m });

        var saved = Assert.IsType<ActivityPreset>(new JsonActivityPresetStore(filePath).Get(preset.Code));

        Assert.Equal("HEMA, интенсивно", saved.Name);
        Assert.Equal(8m, saved.MetValue);
        Assert.Single(new JsonActivityPresetStore(filePath).GetAll());
    }

    [Fact]
    public void Delete_Preset_PersistsDeletion() {
        var store = new JsonActivityPresetStore(filePath);
        var preset = new ActivityPreset("custom:test", "HEMA", 7m);

        store.Save(preset);

        Assert.True(store.Delete(preset.Code));
        Assert.Null(new JsonActivityPresetStore(filePath).Get(preset.Code));
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
