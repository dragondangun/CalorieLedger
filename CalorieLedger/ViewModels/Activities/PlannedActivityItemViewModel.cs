using CalorieLedger.Domain.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class PlannedActivityItemViewModel:ViewModelBase {
    private readonly Action<Guid> edit;
    private readonly Action<Guid> complete;
    private readonly Action<Guid> delete;

    public Guid Id { get; }
    public string Name { get; }
    public string DateSummary { get; }
    public string DetailsSummary { get; }
    public bool CanBeCompleted { get; }

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public PlannedActivityItemViewModel(
        PlannedActivity activity,
        DateOnly currentDate,
        Action<Guid> edit,
        Action<Guid> complete,
        Action<Guid> delete
    ) {
        Id = activity.Id;
        Name = activity.Name;
        DateSummary = activity.Date == currentDate
            ? $"Сегодня · {activity.Date:dd.MM}"
            : $"{activity.Date:dd.MM.yyyy}";
        DetailsSummary = FormatDetails(activity);
        CanBeCompleted = activity.Date <= currentDate;

        this.edit = edit;
        this.complete = complete;
        this.delete = delete;
    }

    [RelayCommand]
    private void Edit() {
        edit(Id);
    }

    [RelayCommand(CanExecute = nameof(CanComplete))]
    private void Complete() {
        complete(Id);
    }

    private bool CanComplete() {
        return CanBeCompleted;
    }

    [RelayCommand]
    private void Delete() {
        IsDeleteConfirmationVisible = true;
    }

    [RelayCommand]
    private void ConfirmDelete() {
        IsDeleteConfirmationVisible = false;
        delete(Id);
    }

    [RelayCommand]
    private void CancelDelete() {
        IsDeleteConfirmationVisible = false;
    }

    partial void OnIsDeleteConfirmationVisibleChanged(bool value) {
        OnPropertyChanged(nameof(ArePrimaryActionsVisible));
    }

    private static string FormatDetails(PlannedActivity activity) {
        var parts = new List<string>();

        if(activity.PlannedAt is not null) {
            parts.Add(activity.PlannedAt.Value.ToString("HH:mm"));
        }

        if(activity.Duration is not null) {
            parts.Add($"{activity.Duration.Value.TotalMinutes:0} мин");
        }

        if(activity.MetValue is not null) {
            parts.Add($"{activity.MetValue:0.#} MET");
        }
        else if(activity.ManualBurnedCaloriesKcal is not null) {
            parts.Add($"{activity.ManualBurnedCaloriesKcal:0} ккал");
        }

        return string.Join(" · ", parts);
    }
}
