namespace CalorieLedger.Application.Activities;

public sealed class InMemoryActivityPresetStore:IActivityPresetStore {
    private readonly List<ActivityPreset> presets = [];

    public IReadOnlyList<ActivityPreset> GetAll() {
        return [
            .. presets.OrderBy(preset => preset.Name).ThenBy(preset => preset.Code)
        ];
    }

    public ActivityPreset? Get(string code) {
        return presets.FirstOrDefault(preset => preset.Code == code);
    }

    public void Save(ActivityPreset preset) {
        ArgumentNullException.ThrowIfNull(preset);

        var index = presets.FindIndex(existing => existing.Code == preset.Code);

        if(index >= 0) {
            presets[index] = preset;
            return;
        }

        presets.Add(preset);
    }

    public bool Delete(string code) {
        return presets.RemoveAll(preset => preset.Code == code) > 0;
    }
}
