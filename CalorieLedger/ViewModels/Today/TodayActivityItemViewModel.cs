using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CalorieLedger.ViewModels.Today;

public sealed partial class TodayActivityItemViewModel:ViewModelBase {
    private readonly Action<Guid> edit;
    private readonly Action<Guid> delete;

    public Guid Id { get; }

    public string Name { get; }

    public decimal BurnedCaloriesKcal { get; }

    public string TimeSummary { get; }

    public string DurationSummary { get; }

    public string? Note { get; }

    public string CaloriesSummary => $"{BurnedCaloriesKcal:0} ккал";

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public TodayActivityItemViewModel(
        Guid id,
        string name,
        decimal burnedCaloriesKcal,
        string timeSummary,
        string durationSummary,
        string? note,
        Action<Guid> edit,
        Action<Guid> delete
    ) {
        ArgumentNullException.ThrowIfNull(edit);
        ArgumentNullException.ThrowIfNull(delete);

        Id = id;
        Name = name;
        BurnedCaloriesKcal = burnedCaloriesKcal;
        TimeSummary = timeSummary;
        DurationSummary = durationSummary;
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
}
