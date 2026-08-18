namespace CalorieLedger.Application.Activities;

public sealed class ActivityPresetCatalogService {
    private readonly IActivityPresetStore presetStore;

    public ActivityPresetCatalogService(IActivityPresetStore presetStore) {
        ArgumentNullException.ThrowIfNull(presetStore);
        this.presetStore = presetStore;
    }

    public IReadOnlyList<ActivityPreset> GetAll() {
        return [
            .. BuiltInActivityPresetCatalog.All
                .Concat(GetCustom())
                .OrderBy(preset => preset.IsBuiltIn)
                .ThenBy(preset => preset.Name)
                .ThenBy(preset => preset.Code)
        ];
    }

    public IReadOnlyList<ActivityPreset> GetCustom() {
        return [
            .. presetStore.GetAll()
                .Select(preset => preset with { IsBuiltIn = false })
                .OrderBy(preset => preset.Name)
                .ThenBy(preset => preset.Code)
        ];
    }

    public ActivityPreset? Find(string? code) {
        if(code is null) {
            return null;
        }

        var builtIn = BuiltInActivityPresetCatalog.Find(code);

        if(builtIn is not null) {
            return builtIn;
        }

        var custom = presetStore.Get(code);
        return custom is null ? null : custom with { IsBuiltIn = false };
    }

    public ActivityPresetDraft CreateNew() {
        return new($"custom:{Guid.NewGuid():N}", string.Empty, null);
    }

    public ActivityPresetDraft? LoadCustom(string code) {
        var preset = presetStore.Get(code);

        return preset is null
            ? null
            : new ActivityPresetDraft(preset.Code, preset.Name, preset.MetValue);
    }

    public ActivityPresetSaveResult Save(ActivityPresetDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = Validate(draft);

        if(errors.Count > 0) {
            return new(false, errors);
        }

        presetStore.Save(
            new ActivityPreset(
                draft.Code,
                draft.Name.Trim(),
                draft.MetValue!.Value,
                IsBuiltIn: false
            )
        );

        return new(true, []);
    }

    public bool Delete(string code) {
        return BuiltInActivityPresetCatalog.Find(code) is null && presetStore.Delete(code);
    }

    private IReadOnlyList<ActivityPresetValidationError> Validate(ActivityPresetDraft draft) {
        var errors = new List<ActivityPresetValidationError>();

        if(string.IsNullOrWhiteSpace(draft.Code)) {
            errors.Add(ActivityPresetValidationError.MissingCode);
        }

        if(BuiltInActivityPresetCatalog.Find(draft.Code) is not null) {
            errors.Add(ActivityPresetValidationError.BuiltInPresetCannotBeChanged);
        }

        if(string.IsNullOrWhiteSpace(draft.Name)) {
            errors.Add(ActivityPresetValidationError.MissingName);
        }

        if(draft.MetValue is null or < 1m) {
            errors.Add(ActivityPresetValidationError.InvalidMetValue);
        }

        if(!string.IsNullOrWhiteSpace(draft.Name)
            && GetAll().Any(preset =>
                preset.Code != draft.Code
                && string.Equals(preset.Name, draft.Name.Trim(), StringComparison.OrdinalIgnoreCase)
            )) {
            errors.Add(ActivityPresetValidationError.DuplicateName);
        }

        return errors;
    }
}
