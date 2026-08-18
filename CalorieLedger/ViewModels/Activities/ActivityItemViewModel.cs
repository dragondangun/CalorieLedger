using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class ActivityItemViewModel:ViewModelBase {
    private readonly Action<Guid> edit;
    private readonly Action<Guid> delete;

    public Guid Id { get; }
    public string Name { get; }
    public decimal BurnedCaloriesKcal { get; }
    public string? Note { get; }

    public string CaloriesSummary => $"{BurnedCaloriesKcal:0} ккал";
    public string TimeSummary { get; }
    public string DurationSummary { get; }
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public ActivityItemViewModel(
        Guid id,
        string name,
        decimal burnedCaloriesKcal,
        TimeOnly? startedAt,
        TimeSpan? duration,
        string? note,
        Action<Guid> edit,
        Action<Guid> delete
    ) {
        ArgumentNullException.ThrowIfNull(edit);
        ArgumentNullException.ThrowIfNull(delete);

        Id = id;
        Name = name;
        BurnedCaloriesKcal = burnedCaloriesKcal;
        TimeSummary = startedAt?.ToString("HH:mm") ?? string.Empty;
        DurationSummary = FormatDuration(duration);
        Note = note;

        this.edit = edit;
        this.delete = delete;
    }

    [RelayCommand]
    private void Edit() {
        edit(Id);
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

    private static string FormatDuration(TimeSpan? duration) {
        if(duration is null) {
            return string.Empty;
        }

        return duration.Value.TotalHours >= 1
            ? $"{duration.Value.TotalHours:0.#} ч"
            : $"{duration.Value.TotalMinutes:0} мин";
    }
}
