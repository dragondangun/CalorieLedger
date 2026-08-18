using CalorieLedger.Application.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class RecurringPlannedActivityManagerViewModel:ViewModelBase {
    private readonly RecurringPlannedActivityService service;
    private readonly ActivityPresetCatalogService presetCatalogService;
    private readonly DateOnly currentDate;
    private readonly Action onChanged;
    private readonly Action onClosed;

    [ObservableProperty]
    private RecurringPlannedActivityEditorViewModel? editor;

    public ObservableCollection<RecurringPlannedActivityScheduleItemViewModel> Schedules { get; } = [];

    public bool IsEditorOpen => Editor is not null;
    public bool IsListVisible => Editor is null;
    public bool HasSchedules => Schedules.Count > 0;
    public bool HasNoSchedules => Schedules.Count == 0;

    public RecurringPlannedActivityManagerViewModel(
        RecurringPlannedActivityService service,
        ActivityPresetCatalogService presetCatalogService,
        DateOnly currentDate,
        Action onChanged,
        Action onClosed
    ) {
        this.service = service;
        this.presetCatalogService = presetCatalogService;
        this.currentDate = currentDate;
        this.onChanged = onChanged;
        this.onClosed = onClosed;

        Refresh();
    }

    [RelayCommand]
    private void Add() {
        OpenEditor(service.CreateNew(currentDate), true);
    }

    [RelayCommand]
    private void Close() {
        onClosed();
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
            onChanged();
        }
    }

    private void OpenEditor(
        RecurringPlannedActivityDraft draft,
        bool isNew
    ) {
        Editor = new RecurringPlannedActivityEditorViewModel(
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
        onChanged();
    }

    private void CloseEditor() {
        Editor = null;
    }

    private void Refresh() {
        Schedules.Clear();

        foreach(var schedule in service.GetAll()) {
            Schedules.Add(
                new RecurringPlannedActivityScheduleItemViewModel(
                    schedule,
                    OpenEditor,
                    Delete
                )
            );
        }

        OnPropertyChanged(nameof(HasSchedules));
        OnPropertyChanged(nameof(HasNoSchedules));
    }

    partial void OnEditorChanged(RecurringPlannedActivityEditorViewModel? value) {
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsListVisible));
    }
}
