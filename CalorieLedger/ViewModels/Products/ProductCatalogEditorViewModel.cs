using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace CalorieLedger.ViewModels.Products;

public partial class ProductCatalogEditorViewModel:ViewModelBase {
    private readonly ProductCatalogService productCatalogService;
    private readonly Guid productId;
    private readonly Action onSaved;
    private readonly Action onCancelled;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? brand;

    [ObservableProperty]
    private string? barcode;

    [ObservableProperty]
    private NutritionBasis nutritionBasis;

    [ObservableProperty]
    private SelectionOption<NutritionBasis>? selectedNutritionBasisOption;

    [ObservableProperty]
    private decimal? caloriesKcal;

    [ObservableProperty]
    private decimal? proteinG;

    [ObservableProperty]
    private decimal? fatG;

    [ObservableProperty]
    private decimal? carbsG;

    public string Title { get; }

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public bool HasValidationErrors => ValidationMessages.Count > 0;

    public IReadOnlyList<SelectionOption<NutritionBasis>> NutritionBasisOptions { get; } = [
        new(
            NutritionBasis.Per100Grams,
            "На 100 г"
        ),
        new(
            NutritionBasis.Per100Milliliters,
            "На 100 мл"
        ),
        new(
            NutritionBasis.PerItem,
            "На 1 штуку"
        ),
    ];

    public ProductCatalogEditorViewModel(
        ProductCatalogService productCatalogService,
        ProductCatalogDraft draft,
        bool isNew,
        Action onSaved,
        Action onCancelled
    ) {
        ArgumentNullException.ThrowIfNull(productCatalogService);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(onSaved);
        ArgumentNullException.ThrowIfNull(onCancelled);

        this.productCatalogService = productCatalogService;
        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        productId = draft.Id;

        Title = isNew ? "Новый продукт" : "Редактирование продукта";

        Name = draft.Name;
        Brand = draft.Brand;
        Barcode = draft.Barcode;
        NutritionBasis = draft.NutritionBasis;
        CaloriesKcal = draft.CaloriesKcal;

        ProteinG = draft.ProteinG;
        FatG = draft.FatG;
        CarbsG = draft.CarbsG;

        SelectedNutritionBasisOption = NutritionBasisOptions.FirstOrDefault(option => option.Value == NutritionBasis);
    }

    partial void OnNutritionBasisChanged(NutritionBasis value) {
        var option = NutritionBasisOptions.FirstOrDefault(existing => existing.Value == value);

        if(SelectedNutritionBasisOption != option) {
            SelectedNutritionBasisOption = option;
        }
    }

    partial void OnSelectedNutritionBasisOptionChanged(SelectionOption<NutritionBasis>? value) {
        if(value is not null
            && NutritionBasis != value.Value) {
            NutritionBasis = value.Value;
        }
    }

    [RelayCommand]
    private void Save() {
        ClearValidationMessages();

        var result = productCatalogService.Save(
            new ProductCatalogDraft(
                Id: productId,
                Name: Name,
                NutritionBasis: NutritionBasis,
                CaloriesKcal: CaloriesKcal,
                ProteinG: ProteinG,
                FatG: FatG,
                CarbsG: CarbsG,
                Brand: Brand,
                Barcode: Barcode
            )
        );

        if(result.IsSuccess) {
            onSaved();
            return;
        }

        foreach(var error in result.Errors) {
            ValidationMessages.Add(FormatValidationError(error));
        }

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    [RelayCommand]
    private void Cancel() {
        onCancelled();
    }

    private void ClearValidationMessages() {
        ValidationMessages.Clear();

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    private static string FormatValidationError(ProductCatalogValidationError error) {
        return error switch {
            ProductCatalogValidationError.MissingId => "Не удалось определить продукт.",
            ProductCatalogValidationError.MissingName => "Введите название продукта.",
            ProductCatalogValidationError.InvalidNutritionBasis => "Выберите способ задания КБЖУ.",
            ProductCatalogValidationError.InvalidCalories => "Калорийность не может быть отрицательной.",
            ProductCatalogValidationError.InvalidProtein => "Количество белка не может быть отрицательным.",
            ProductCatalogValidationError.InvalidFat => "Количество жира не может быть отрицательным.",
            ProductCatalogValidationError.InvalidCarbs => "Количество углеводов не может быть отрицательным.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error,
                null
            )
        };
    }
}
