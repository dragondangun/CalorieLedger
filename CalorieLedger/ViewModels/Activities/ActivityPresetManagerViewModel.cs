using CalorieLedger.Application.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class ActivityPresetManagerViewModel:ViewModelBase {
    private readonly ActivityPresetCatalogService presetService;
    private readonly Action onChanged;
    private readonly Action onClosed;

    [ObservableProperty]
    private ActivityPresetEditorViewModel? editor;

    public ObservableCollection<ActivityPresetListItemViewModel> Presets { get; } = [];
    public bool IsEditorOpen => Editor is not null;
    public bool IsListVisible => Editor is null;
    public bool HasPresets => Presets.Count > 0;
    public bool HasNoPresets => Presets.Count == 0;

    public ActivityPresetManagerViewModel(
        ActivityPresetCatalogService presetService,
        Action onChanged,
        Action onClosed
    ) {
        this.presetService = presetService;
        this.onChanged = onChanged;
        this.onClosed = onClosed;
        Refresh();
    }

    [RelayCommand]
    private void AddPreset() {
        OpenEditor(presetService.CreateNew(), true);
    }

    [RelayCommand]
    private void Close() {
        onClosed();
    }

    private void EditPreset(string code) {
        var draft = presetService.LoadCustom(code);

        if(draft is null) {
            Refresh();
            return;
        }

        OpenEditor(draft, false);
    }

    private void DeletePreset(string code) {
        if(presetService.Delete(code)) {
            Refresh();
            onChanged();
        }
    }

    private void OpenEditor(ActivityPresetDraft draft, bool isNew) {
        Editor = new ActivityPresetEditorViewModel(
            presetService,
            draft,
            isNew,
            OnEditorSaved,
            CloseEditor
        );
    }

    private void OnEditorSaved() {
        Editor = null;
        Refresh();
        onChanged();
    }

    private void CloseEditor() {
        Editor = null;
    }

    private void Refresh() {
        Presets.Clear();

        foreach(var preset in presetService.GetCustom()) {
            Presets.Add(
                new ActivityPresetListItemViewModel(
                    preset.Code,
                    preset.Name,
                    preset.MetValue,
                    EditPreset,
                    DeletePreset
                )
            );
        }

        OnPropertyChanged(nameof(HasPresets));
        OnPropertyChanged(nameof(HasNoPresets));
    }

    partial void OnEditorChanged(ActivityPresetEditorViewModel? value) {
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsListVisible));
    }
}
