using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CalorieLedger.ViewModels.Fridge;

public partial class FridgeManagerViewModel:ViewModelBase {
    private readonly FridgeInventoryService fridgeInventoryService;
    private readonly ProductCatalogService productCatalogService;
    private readonly DateOnly currentDate;
    private readonly Action<Guid> logFood;
    private readonly Action onClosed;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string catalogSearchQuery = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ProductCatalogItem> catalogResults = [];

    [ObservableProperty]
    private ProductCatalogItem? selectedCatalogProduct;

    [ObservableProperty]
    private decimal? catalogQuantityValue;

    [ObservableProperty]
    private DateTimeOffset? expirationDate;

    [ObservableProperty]
    private string? note;

    [ObservableProperty]
    private string actionSummary = string.Empty;

    public ObservableCollection<FridgeItemViewModel> Items { get; } = [];

    public bool HasItems => Items.Count > 0;

    public bool HasNoItems => Items.Count == 0;

    public FridgeManagerViewModel(
        FridgeInventoryService fridgeInventoryService,
        ProductCatalogService productCatalogService,
        DateOnly currentDate,
        Action<Guid> logFood,
        Action onClosed
    ) {
        ArgumentNullException.ThrowIfNull(fridgeInventoryService);
        ArgumentNullException.ThrowIfNull(productCatalogService);
        ArgumentNullException.ThrowIfNull(logFood);
        ArgumentNullException.ThrowIfNull(onClosed);

        this.fridgeInventoryService = fridgeInventoryService;
        this.productCatalogService = productCatalogService;

        this.currentDate = currentDate;

        this.logFood = logFood;

        this.onClosed = onClosed;

        RefreshCatalogResults();
        RefreshItems();
    }

    partial void OnSearchQueryChanged(string value) {
        RefreshItems();
    }

    partial void OnCatalogSearchQueryChanged(string value) {
        RefreshCatalogResults();
    }

    partial void OnSelectedCatalogProductChanged(ProductCatalogItem? value) {
        if(value is null) {
            return;
        }

        CatalogQuantityValue = value.Nutrition.Basis switch {
            NutritionBasis.Per100Grams => 100m,
            NutritionBasis.Per100Milliliters => 100m,
            NutritionBasis.PerItem => 1m,
            _ => null
        };
    }

    [RelayCommand]
    private void AddCatalogProduct() {
        if(SelectedCatalogProduct is null || CatalogQuantityValue is not > 0m) {
            ActionSummary = "Выберите продукт и укажите количество.";

            return;
        }

        var result = fridgeInventoryService.AddCatalogProduct(
            product: SelectedCatalogProduct,
            quantityValue: CatalogQuantityValue.Value,
            expirationDate: GetExpirationDate(),
            note: Note
        );

        if(!result.IsSuccess) {
            ActionSummary = FormatErrors(result.Errors);

            return;
        }

        ActionSummary = $"«{result.Item!.Name}» добавлен в холодильник.";

        SelectedCatalogProduct = null;

        CatalogQuantityValue = null;

        ResetSharedFields();
        RefreshItems();
    }

    [RelayCommand]
    private void Close() {
        onClosed();
    }

    public void RefreshItems() {
        Items.Clear();

        foreach(var item in fridgeInventoryService.Search(SearchQuery)) {
            Items.Add(
                new FridgeItemViewModel(
                    item: item,
                    currentDate: currentDate,
                    logFood: logFood,
                    delete: DeleteItem
                )
            );
        }

        OnPropertyChanged(nameof(HasItems));

        OnPropertyChanged(nameof(HasNoItems));
    }

    private void DeleteItem(Guid id) {
        fridgeInventoryService.Delete(id);

        RefreshItems();
    }

    private void RefreshCatalogResults() {
        CatalogResults = productCatalogService.Search(CatalogSearchQuery);
    }

    private DateOnly? GetExpirationDate() {
        return ExpirationDate is null ? null : DateOnly.FromDateTime(ExpirationDate.Value.Date);
    }

    private void ResetSharedFields() {
        ExpirationDate = null;
        Note = null;
    }

    private static string FormatErrors(IReadOnlyList<FridgeItemValidationError> errors) {
        return string.Join(
            " ",
            errors.Select(FormatError)
        );
    }

    private static string FormatError(FridgeItemValidationError error) {
        return error switch {
            FridgeItemValidationError.InvalidQuantity => "Количество должно быть больше 0.",
            FridgeItemValidationError.UnsupportedNutritionBasis => "Этот способ задания КБЖУ нельзя хранить как остаток.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error,
                null
            )
        };
    }
}
