using CalorieLedger.Application.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class PlannedActivityManagerViewModel:ViewModelBase {
    private readonly PlannedActivityService service;
    private readonly ActivityPresetCatalogService presetCatalogService;
    private readonly DateOnly currentDate;
    private readonly Action<Guid> complete;
    private readonly Action onClosed;
    private readonly Action? onChanged;
    private readonly Action openRecurringActivities;

    [ObservableProperty]
    private PlannedActivityEditorViewModel? editor;

    public ObservableCollection<PlannedActivityItemViewModel> Activities { get; } = [];
    public bool IsEditorOpen => Editor is not null;
    public bool IsListVisible => Editor is null;
    public bool HasActivities => Activities.Count > 0;
    public bool HasNoActivities => Activities.Count == 0;

    public PlannedActivityManagerViewModel(
        PlannedActivityService service,
        ActivityPresetCatalogService presetCatalogService,
        DateOnly currentDate,
        Action<Guid> complete,
        Action openRecurringActivities,
        Action onClosed,
        Action? onChanged = null
    ) {
        this.service = service;
        this.presetCatalogService = presetCatalogService;
        this.currentDate = currentDate;
        this.complete = complete;
        this.onClosed = onClosed;
        this.onChanged = onChanged;
        this.openRecurringActivities = openRecurringActivities;

        Refresh();
    }

    public void Refresh() {
        Activities.Clear();

        foreach(var activity in service.GetAll()) {
            Activities.Add(
                new PlannedActivityItemViewModel(
                    activity,
                    currentDate,
                    Edit,
                    complete,
                    Delete
                )
            );
        }

        OnPropertyChanged(nameof(HasActivities));
        OnPropertyChanged(nameof(HasNoActivities));
    }

    [RelayCommand]
    private void Add() {
        OpenEditor(service.CreateNew(currentDate), true);
    }

    [RelayCommand]
    private void Close() {
        onClosed();
    }
    [RelayCommand]
    private void OpenRecurringActivities() {
        openRecurringActivities();
    }

    private void Edit(Guid id) {
        OpenEditor(id);
    }

    public void OpenEditor(Guid id) {
        var draft = service.Load(id);

        if(draft is not null) {
            OpenEditor(draft, false);
        }
    }

    private void Delete(Guid id) {
        if(service.Delete(id)) {
            Refresh();
            onChanged?.Invoke();
        }
    }

    private void OpenEditor(PlannedActivityDraft draft, bool isNew) {
        Editor = new PlannedActivityEditorViewModel(
            service,
            presetCatalogService,
            draft,
            isNew,
            OnEditorSaved,
            CloseEditor
        );
    }

    private void OnEditorSaved() {
        Editor = null;
        Refresh();
        onChanged?.Invoke();
    }

    private void CloseEditor() {
        Editor = null;
    }

    partial void OnEditorChanged(PlannedActivityEditorViewModel? value) {
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsListVisible));
    }
}
