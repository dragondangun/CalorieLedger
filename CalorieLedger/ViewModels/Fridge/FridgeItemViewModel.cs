using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using CalorieLedger.Domain.Common;
using System.Globalization;

namespace CalorieLedger.ViewModels.Fridge;

public sealed partial class FridgeItemViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly Action<Guid> logFood;
    private readonly Action<Guid> delete;

    public Guid Id { get; }

    public string Name { get; }

    public string QuantitySummary { get; }

    public string NutritionSummary { get; }

    public string ExpirationSummary { get; }

    public string SourceSummary { get; }

    public bool IsEmpty { get; }

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public FridgeItemViewModel(
        FridgeItem item,
        DateOnly currentDate,
        Action<Guid> logFood,
        Action<Guid> delete
    ) {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(logFood);
        ArgumentNullException.ThrowIfNull(delete);

        Id = item.Id;
        Name = item.Name;
        QuantitySummary = $"{item.Quantity.Value.ToString("0.##", RussianCulture)} {FormatUnit(item.Quantity.Unit)}";

        var totals = NutritionCalculator.CalculateTotal(item.Nutrition, item.Quantity);

        NutritionSummary = $"В остатке: {FormatValue(totals.CaloriesKcal)} ккал · Б: {FormatValue(totals.ProteinG)} г · Ж: {FormatValue(totals.FatG)} г · У: {FormatValue(totals.CarbsG)} г";

        ExpirationSummary = FormatExpiration(
            item.ExpirationDate,
            currentDate
        );

        SourceSummary = item.Source switch {
            FridgeItemSource.Manual => "ручная запись",
            FridgeItemSource.CatalogProduct => "из каталога",
            FridgeItemSource.CookingSession => "приготовленное блюдо",
            _ => throw new ArgumentOutOfRangeException(
                nameof(item),
                item.Source,
                null
            )
        };

        IsEmpty = item.Quantity.Value <= 0m;

        this.logFood = logFood;
        this.delete = delete;
    }

    [RelayCommand(CanExecute = nameof(CanLogFood))]
    private void LogFood() {
        logFood(Id);
    }

    private bool CanLogFood() {
        return !IsEmpty;
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

    private static string FormatExpiration(
        DateOnly? expirationDate,
        DateOnly currentDate
    ) {
        if(expirationDate is null) {
            return "срок годности не указан";
        }

        var daysLeft = expirationDate.Value.DayNumber - currentDate.DayNumber;

        return daysLeft switch {
            < 0 => $"просрочено на {-daysLeft} дн.",
            0 => "срок годности истекает сегодня",
            1 => "остался 1 день",
            _ => $"осталось {daysLeft} дн."
        };
    }

    private static string FormatUnit(FoodUnit unit) {
        return unit switch {
            FoodUnit.Gram => "г",
            FoodUnit.Milliliter => "мл",
            FoodUnit.Piece => "шт.",
            FoodUnit.Portion => "порц.",
            _ => unit.ToString()
        };
    }

    private static string FormatValue(decimal? value) {
        return value is null ? "—" : value.Value.ToString("0.##", RussianCulture);
    }
}
