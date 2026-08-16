using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;

namespace CalorieLedger.ViewModels.Products;

public sealed partial class ProductCatalogListItemViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly Action<Guid> onEdit;
    private readonly Action<Guid> onDelete;

    public Guid Id { get; }
    public string Name { get; }
    public string? Brand { get; }
    public string? Barcode { get; }
    public string NutritionSummary { get; }

    public bool HasBrand => !string.IsNullOrWhiteSpace(Brand);

    public bool HasBarcode => !string.IsNullOrWhiteSpace(Barcode);

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public ProductCatalogListItemViewModel(
        ProductCatalogItem item,
        Action<Guid> onEdit,
        Action<Guid> onDelete
    ) {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(onEdit);
        ArgumentNullException.ThrowIfNull(onDelete);

        Id = item.Id;
        Name = item.Name;
        Brand = item.Brand;
        Barcode = item.Barcode;

        NutritionSummary = FormatNutrition(item.Nutrition);

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
        OnPropertyChanged(nameof(ArePrimaryActionsVisible));
    }

    private static string FormatNutrition(NutritionFacts nutrition) {
        return $"{FormatValue(nutrition.CaloriesKcal)} ккал · Б: {FormatValue(nutrition.ProteinG)} г · Ж: {FormatValue(nutrition.FatG)} г · У: {FormatValue(nutrition.CarbsG)} г · {FormatBasis(nutrition.Basis)}";
    }

    private static string FormatValue(decimal? value) {
        return value is null
            ? "—"
            : value.Value.ToString("0.##", RussianCulture);
    }

    private static string FormatBasis(NutritionBasis basis) {
        return basis switch {
            NutritionBasis.Per100Grams => "на 100 г",
            NutritionBasis.Per100Milliliters => "на 100 мл",
            NutritionBasis.PerItem => "на 1 шт.",
            NutritionBasis.Total => "на всё количество",
            _ => throw new ArgumentOutOfRangeException(
                nameof(basis),
                basis,
                null
            )
        };
    }
}
