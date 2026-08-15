using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CalorieLedger.ViewModels.Today;

public sealed partial class TodayFoodLogItemViewModel:ViewModelBase {
    private readonly Action<Guid> onEdit;
    private readonly Action<Guid> onDelete;

    public Guid Id { get; }
    public string Name { get; }
    public string QuantitySummary { get; }
    public string CaloriesSummary { get; }
    public string MacrosSummary { get; }
    public bool IsApproximate { get; }
    public decimal? CaloriesKcal { get; }
    public decimal? ProteinG { get; }
    public decimal? FatG { get; }
    public decimal? CarbsG { get; }

    public string AccuracySummary => IsApproximate
        ? "примерная оценка"
        : "точная запись";

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public TodayFoodLogItemViewModel(
        Guid id,
        string name,
        string quantitySummary,
        string caloriesSummary,
        string macrosSummary,
        Action<Guid> onEdit,
        Action<Guid> onDelete,
        bool isApproximate = false,
        decimal? caloriesKcal = null,
        decimal? proteinG = null,
        decimal? fatG = null,
        decimal? carbsG = null
    ) {
        ArgumentNullException.ThrowIfNull(onEdit);
        ArgumentNullException.ThrowIfNull(onDelete);

        Id = id;
        Name = name;
        QuantitySummary = quantitySummary;
        CaloriesSummary = caloriesSummary;
        MacrosSummary = macrosSummary;
        IsApproximate = isApproximate;
        CaloriesKcal = caloriesKcal;
        ProteinG = proteinG;
        FatG = fatG;
        CarbsG = carbsG;

        this.onEdit = onEdit;
        this.onDelete = onDelete;
    }

    [RelayCommand]
    private void Edit() {
        onEdit(Id);
    }

    [RelayCommand]
    private void Delete() {
        IsDeleteConfirmationVisible = true;
    }

    [RelayCommand]
    private void ConfirmDelete() {
        IsDeleteConfirmationVisible = false;

        onDelete(Id);
    }

    [RelayCommand]
    private void CancelDelete() {
        IsDeleteConfirmationVisible = false;
    }

    partial void OnIsDeleteConfirmationVisibleChanged(bool value) {
        OnPropertyChanged(
            nameof(ArePrimaryActionsVisible)
        );
    }
}
