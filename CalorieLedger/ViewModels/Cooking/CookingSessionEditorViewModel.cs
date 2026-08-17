using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CalorieLedger.ViewModels.Cooking;

public partial class CookingSessionEditorViewModel:ViewModelBase {
    private const int MaxCatalogResultCount = 20;

    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly CookingSessionService cookingSessionService;
    private readonly ProductCatalogService productCatalogService;
    private readonly Guid sessionId;
    private readonly List<CookingIngredient> ingredients;
    private readonly Action onSaved;
    private readonly Action onCancelled;
    private readonly FridgeInventoryService fridgeInventoryService;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private decimal outputWeightG;

    [ObservableProperty]
    private string? note;

    [ObservableProperty]
    private string catalogSearchQuery = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ProductCatalogItem> catalogResults = [];

    [ObservableProperty]
    private ProductCatalogItem? selectedCatalogProduct;

    [ObservableProperty]
    private decimal? ingredientQuantityValue;

    [ObservableProperty]
    private string catalogActionSummary = "Выберите продукт из каталога и укажите использованное количество.";

    [ObservableProperty]
    private string totalNutritionSummary = "Добавьте ингредиенты и укажите вес готового блюда.";

    [ObservableProperty]
    private string nutritionPer100GramsSummary = string.Empty;
    [ObservableProperty]
    private string fridgeSearchQuery = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<FridgeItem> fridgeResults = [];

    [ObservableProperty]
    private FridgeItem? selectedFridgeItem;

    [ObservableProperty]
    private decimal? fridgeQuantityValue;

    [ObservableProperty]
    private string fridgeActionSummary = "Выберите остаток и укажите используемое количество.";

    public string Title { get; }

    public ObservableCollection<CookingIngredientItemViewModel> Ingredients { get; } = [];

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public bool HasValidationErrors => ValidationMessages.Count > 0;

    public bool HasIngredients => Ingredients.Count > 0;

    public CookingSessionEditorViewModel(
        CookingSessionService cookingSessionService,
        ProductCatalogService productCatalogService,
        FridgeInventoryService fridgeInventoryService,
        CookingSessionDraft draft,
        bool isNew,
        Action onSaved,
        Action onCancelled
    ) {
        ArgumentNullException.ThrowIfNull(cookingSessionService);
        ArgumentNullException.ThrowIfNull(productCatalogService);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(onSaved);
        ArgumentNullException.ThrowIfNull(onCancelled);
        ArgumentNullException.ThrowIfNull(fridgeInventoryService);

        this.cookingSessionService = cookingSessionService;
        this.productCatalogService = productCatalogService;
        this.fridgeInventoryService = fridgeInventoryService;

        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        sessionId = draft.Id;

        ingredients = [
            .. draft.Ingredients,
        ];

        Title = isNew ? "Новое приготовление" : "Редактирование приготовления";

        Name = draft.Name;
        outputWeightG = draft.OutputWeightG;
        Note = draft.Note;

        RefreshCatalogResults();
        RefreshIngredientItems();
        RefreshFridgeResults();
        UpdatePreview();
    }

    partial void OnCatalogSearchQueryChanged(string value) {
        RefreshCatalogResults();
    }

    partial void OnSelectedCatalogProductChanged(ProductCatalogItem? value) {
        if(value is null) {
            return;
        }

        IngredientQuantityValue = value.Nutrition.Basis switch {
            NutritionBasis.Per100Grams => 100m,
            NutritionBasis.Per100Milliliters => 100m,
            NutritionBasis.PerItem => 1m,
            _ => null
        };
    }

    partial void OnOutputWeightGChanged(decimal value) {
        UpdatePreview();
    }

    [RelayCommand]
    private void AddCatalogIngredient() {
        if(SelectedCatalogProduct is null) {
            CatalogActionSummary = "Сначала выберите продукт.";

            return;
        }

        if(IngredientQuantityValue is not > 0m) {
            CatalogActionSummary = "Укажите использованное количество больше 0.";

            return;
        }

        var ingredient = cookingSessionService.CreateCatalogIngredient(
            SelectedCatalogProduct,
            IngredientQuantityValue.Value
        );

        if(ingredient is null) {
            CatalogActionSummary = "Этот продукт нельзя использовать с выбранной единицей измерения.";

            return;
        }

        ingredients.Add(ingredient);

        Ingredients.Add(CreateIngredientItem(ingredient));

        CatalogActionSummary = $"Добавлено: {ingredient.Name}.";

        SelectedCatalogProduct = null;

        IngredientQuantityValue = null;

        OnPropertyChanged(nameof(HasIngredients));

        UpdatePreview();
    }

    [RelayCommand]
    private void Save() {
        ClearValidationMessages();

        var result = cookingSessionService.Save(
            CreateDraft()
        );

        if(result.IsSuccess) {
            onSaved();
            return;
        }

        foreach(var error in result.Errors) {
            ValidationMessages.Add(
                FormatValidationError(error)
            );
        }

        OnPropertyChanged(
            nameof(HasValidationErrors)
        );
    }

    [RelayCommand]
    private void Cancel() {
        onCancelled();
    }

    [RelayCommand]
    private void AddFridgeIngredient() {
        if(SelectedFridgeItem is null) {
            FridgeActionSummary = "Сначала выберите остаток из холодильника.";

            return;
        }

        if(FridgeQuantityValue is not > 0m) {
            FridgeActionSummary = "Укажите количество больше 0.";

            return;
        }

        if(FridgeQuantityValue.Value > SelectedFridgeItem.Quantity.Value) {
            FridgeActionSummary = "В холодильнике нет такого количества.";

            return;
        }

        var ingredient = cookingSessionService.CreateFridgeIngredient(
            SelectedFridgeItem,
            FridgeQuantityValue.Value
        );

        if(ingredient is null) {
            FridgeActionSummary = "Этот остаток нельзя использовать как ингредиент.";

            return;
        }

        ingredients.Add(ingredient);

        Ingredients.Add(CreateIngredientItem(ingredient));

        FridgeActionSummary = $"Добавлено: {ingredient.Name}.";

        SelectedFridgeItem = null;

        FridgeQuantityValue = null;

        OnPropertyChanged(nameof(HasIngredients));

        UpdatePreview();
    }
    private CookingSessionDraft CreateDraft() {
        return new CookingSessionDraft(
            Id: sessionId,
            Name: Name,
            Ingredients: [
                .. ingredients,
            ],
            OutputWeightG: OutputWeightG,
            Note: Note
        );
    }

    private void RefreshCatalogResults() {
        CatalogResults = [
            .. productCatalogService
                .Search(CatalogSearchQuery)
                .Take(MaxCatalogResultCount),
        ];
    }

    private void RefreshIngredientItems() {
        Ingredients.Clear();

        foreach(var ingredient in ingredients) {
            Ingredients.Add(CreateIngredientItem(ingredient));
        }

        OnPropertyChanged(nameof(HasIngredients));
    }

    private CookingIngredientItemViewModel CreateIngredientItem(CookingIngredient ingredient) {
        return new CookingIngredientItemViewModel(
            id: ingredient.Id,
            name: ingredient.Name,
            quantity: ingredient.Quantity,
            nutrition: ingredient.Nutrition,
            source: ingredient.Source,
            updateQuantity: UpdateIngredientQuantity,
            remove: RemoveIngredient
        );
    }

    private void UpdateIngredientQuantity(
        Guid id,
        decimal quantityValue
    ) {
        var index = ingredients.FindIndex(
            ingredient => ingredient.Id == id
        );

        if(index < 0) {
            return;
        }

        var ingredient = ingredients[index];

        ingredients[index] = ingredient with {
            Quantity = new FoodQuantity(
                quantityValue,
                ingredient.Quantity.Unit
            ),
        };

        UpdatePreview();
    }

    private void RemoveIngredient(Guid id) {
        ingredients.RemoveAll(ingredient => ingredient.Id == id);

        var item = Ingredients.FirstOrDefault(ingredient => ingredient.Id == id);

        if(item is not null) {
            Ingredients.Remove(item);
        }

        OnPropertyChanged(nameof(HasIngredients));

        UpdatePreview();
    }

    private void UpdatePreview() {
        var preview = cookingSessionService.CalculatePreview(CreateDraft());

        if(preview is null) {
            TotalNutritionSummary = "Добавьте ингредиенты с корректным количеством и укажите вес готового блюда.";

            NutritionPer100GramsSummary = string.Empty;

            return;
        }

        TotalNutritionSummary = $"Всё блюдо: {FormatNutrition(preview.TotalNutrition)}";

        NutritionPer100GramsSummary = $"На 100 г: {FormatNutrition(preview.NutritionPer100Grams)}";
    }

    private void ClearValidationMessages() {
        ValidationMessages.Clear();

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    private static string FormatValidationError(CookingSessionValidationError error) {
        return error switch {
            CookingSessionValidationError.MissingId => "Не удалось определить приготовление.",
            CookingSessionValidationError.MissingName => "Введите название блюда.",
            CookingSessionValidationError.NoIngredients => "Добавьте хотя бы один ингредиент.",
            CookingSessionValidationError.InvalidOutputWeight => "Вес готового блюда должен быть больше 0 г.",
            CookingSessionValidationError.InvalidIngredientId => "Один из ингредиентов имеет некорректный идентификатор.",
            CookingSessionValidationError.MissingIngredientName => "У одного из ингредиентов отсутствует название.",
            CookingSessionValidationError.InvalidIngredientQuantity => "Количество каждого ингредиента должно быть больше 0.",
            CookingSessionValidationError.IncompatibleIngredientNutritionBasis => "Единица измерения одного из ингредиентов не соответствует его КБЖУ.",
            CookingSessionValidationError.InvalidIngredientNutrition => "КБЖУ ингредиента не могут содержать отрицательные значения.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error,
                null
            )
        };
    }

    private static string FormatNutrition(NutritionTotals nutrition) {
        return $"{FormatValue(nutrition.CaloriesKcal)} ккал · Б: {FormatValue(nutrition.ProteinG)} г · Ж: {FormatValue(nutrition.FatG)} г · У: {FormatValue(nutrition.CarbsG)} г";
    }

    private static string FormatNutrition(NutritionFacts nutrition) {
        return $"{FormatValue(nutrition.CaloriesKcal)} ккал · Б: {FormatValue(nutrition.ProteinG)} г · Ж: {FormatValue(nutrition.FatG)} г · У: {FormatValue(nutrition.CarbsG)} г";
    }

    private static string FormatValue(decimal? value) {
        return value is null ? "—" : value.Value.ToString("0.##", RussianCulture);
    }

    partial void OnFridgeSearchQueryChanged(string value) {
        RefreshFridgeResults();
    }

    partial void OnSelectedFridgeItemChanged(FridgeItem? value) {
        if(value is null) {
            return;
        }

        var preferredQuantity = value.Quantity.Unit switch {
            FoodUnit.Gram => 100m,
            FoodUnit.Milliliter => 100m,
            FoodUnit.Piece => 1m,
            FoodUnit.Portion => 1m,
            _ => 1m
        };

        FridgeQuantityValue = Math.Min(
            preferredQuantity,
            value.Quantity.Value
        );
    }

    private void RefreshFridgeResults() {
        FridgeResults = [
            .. fridgeInventoryService
            .Search(FridgeSearchQuery)
            .Where(item => item.Quantity.Value > 0m)
            .Take(MaxCatalogResultCount),
    ];
    }
}
