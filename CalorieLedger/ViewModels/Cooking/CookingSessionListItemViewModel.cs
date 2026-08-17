using CalorieLedger.Domain.Cooking;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;

namespace CalorieLedger.ViewModels.Cooking;

public sealed partial class CookingSessionListItemViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly Action<Guid> logFood;
    private readonly Action<Guid> edit;
    private readonly Action<Guid> delete;

    public Guid Id { get; }

    public string Name { get; }

    public string OutputWeightSummary { get; }

    public string NutritionSummary { get; }

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public CookingSessionListItemViewModel(
        CookingSessionDraft session,
        CookingNutritionResult? nutrition,
        Action<Guid> logFood,
        Action<Guid> edit,
        Action<Guid> delete
    ) {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(logFood);
        ArgumentNullException.ThrowIfNull(edit);
        ArgumentNullException.ThrowIfNull(delete);

        Id = session.Id;
        Name = session.Name;

        OutputWeightSummary = $"{session.OutputWeightG.ToString("0.##", RussianCulture)} г готового блюда";

        NutritionSummary = nutrition is null
            ? "Расчёт КБЖУ недоступен"
            : $"На 100 г: {FormatValue(nutrition.NutritionPer100Grams.CaloriesKcal)} ккал · Б: {FormatValue(nutrition.NutritionPer100Grams.ProteinG)} г · Ж: {FormatValue(nutrition.NutritionPer100Grams.FatG)} г · У: {FormatValue(nutrition.NutritionPer100Grams.CarbsG)} г";

        this.logFood = logFood;

        this.edit = edit;

        this.delete = delete;
    }

    [RelayCommand]
    private void LogFood() {
        logFood(Id);
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

    private static string FormatValue(decimal? value) {
        return value is null ? "—" : value.Value.ToString("0.##", RussianCulture);
    }
}
