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
        Action onClosed
    ) {
        this.service = service;
        this.presetCatalogService = presetCatalogService;
        this.currentDate = currentDate;
        this.complete = complete;
        this.onClosed = onClosed;

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

    private void Edit(Guid id) {
        var draft = service.Load(id);

        if(draft is not null) {
            OpenEditor(draft, false);
        }
    }

    private void Delete(Guid id) {
        if(service.Delete(id)) {
            Refresh();
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
    }

    private void CloseEditor() {
        Editor = null;
    }

    partial void OnEditorChanged(PlannedActivityEditorViewModel? value) {
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsListVisible));
    }
}
