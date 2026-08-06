using CalorieLedger.Domain.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CalorieLedger.ViewModels.Profile;

public partial class BodyMeasurementListItemViewModel:ViewModelBase {
    private readonly Action<Guid> onEdit;
    private readonly Action<Guid> onDelete;
    private readonly Action? onAddMeasurement;

    public Guid Id { get; }

    public string DateSummary { get; }

    public string WeightSummary { get; }

    public string AdditionalValuesSummary { get; }

    public bool HasAdditionalValues => !string.IsNullOrWhiteSpace(AdditionalValuesSummary);

    public string ChangesSummary { get; }

    public bool HasChangesSummary => !string.IsNullOrWhiteSpace(ChangesSummary);

    public bool IsLatest { get; }

    public string LatestBadgeText { get; }

    public bool IsLatestMeasurementStale { get; }

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public bool CanAddMeasurement => IsLatestMeasurementStale && onAddMeasurement is not null;

    public string DataCompletenessText { get; }

    public bool HasDataCompletenessNotice => !string.IsNullOrWhiteSpace(DataCompletenessText);

    public BodyMeasurementListItemViewModel(
        BodyMeasurementEntry entry,
        Action<Guid> onEdit,
        Action<Guid> onDelete,
        BodyMeasurementEntry? previousMeasurement = null,
        bool isLatest = false,
        DateOnly? currentDate = null,
        Action? onAddMeasurement = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(onEdit);
        ArgumentNullException.ThrowIfNull(onDelete);

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: entry,
            previousMeasurement: previousMeasurement,
            isLatest: isLatest,
            currentDate: currentDate
        );

        Id = entry.Id;
        DateSummary = presentation.DateSummary;
        WeightSummary = presentation.WeightSummary;
        AdditionalValuesSummary = presentation.AdditionalValuesSummary;
        ChangesSummary = presentation.ChangesSummary;
        DataCompletenessText = presentation.DataCompletenessText;
        IsLatest = presentation.IsLatest;
        LatestBadgeText = presentation.LatestBadgeText;
        IsLatestMeasurementStale = presentation.IsLatestMeasurementStale;

        this.onEdit = onEdit;
        this.onDelete = onDelete;
        this.onAddMeasurement = onAddMeasurement;
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

    [RelayCommand(CanExecute = nameof(CanAddMeasurement))]
    private void AddMeasurement() {
        onAddMeasurement?.Invoke();
    }

    partial void OnIsDeleteConfirmationVisibleChanged(bool value) {
        OnPropertyChanged(nameof(ArePrimaryActionsVisible));
    }
}