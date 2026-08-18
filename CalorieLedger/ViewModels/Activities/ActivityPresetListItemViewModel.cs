using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class ActivityPresetListItemViewModel:ViewModelBase {
    private readonly Action<string> edit;
    private readonly Action<string> delete;

    public string Code { get; }
    public string Name { get; }
    public decimal MetValue { get; }
    public string MetSummary => $"{MetValue:0.#} MET";

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public ActivityPresetListItemViewModel(
        string code,
        string name,
        decimal metValue,
        Action<string> edit,
        Action<string> delete
    ) {
        Code = code;
        Name = name;
        MetValue = metValue;
        this.edit = edit;
        this.delete = delete;
    }

    [RelayCommand]
    private void Edit() {
        edit(Code);
    }

    [RelayCommand]
    private void Delete() {
        IsDeleteConfirmationVisible = true;
    }

    [RelayCommand]
    private void ConfirmDelete() {
        IsDeleteConfirmationVisible = false;
        delete(Code);
    }

    [RelayCommand]
    private void CancelDelete() {
        IsDeleteConfirmationVisible = false;
    }

    partial void OnIsDeleteConfirmationVisibleChanged(bool value) {
        OnPropertyChanged(nameof(ArePrimaryActionsVisible));
    }
}
