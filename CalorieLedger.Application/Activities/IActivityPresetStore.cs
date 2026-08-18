namespace CalorieLedger.Application.Activities;

public interface IActivityPresetStore {
    IReadOnlyList<ActivityPreset> GetAll();
    ActivityPreset? Get(string code);
    void Save(ActivityPreset preset);
    bool Delete(string code);
}
