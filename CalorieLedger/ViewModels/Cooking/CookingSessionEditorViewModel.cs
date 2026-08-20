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
    private readonly CookingNutritionLlmService cookingNutritionLlmService;
    private readonly ProductCatalogService productCatalogService;
    private readonly Guid sessionId;
    private readonly List<CookingIngredient> ingredients;
    private readonly Action onSaved;
    private readonly Action onCancelled;
    private readonly FridgeInventoryService fridgeInventoryService;
    private NutritionFacts? nutritionPer100GramsOverride;

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

    [ObservableProperty]
    private bool isLlmNutritionPanelVisible;

    [ObservableProperty]
    private string llmRequestText = string.Empty;

    [ObservableProperty]
    private string llmResponseInstructions = string.Empty;

    [ObservableProperty]
    private string llmResponseText = string.Empty;

    [ObservableProperty]
    private string llmActionSummary = string.Empty;

    public bool HasNutritionOverride => nutritionPer100GramsOverride is not null;

    public string NutritionOverrideSummary => nutritionPer100GramsOverride is null
        ? string.Empty
        : $"Используется оценка КБЖУ: {FormatNutrition(nutritionPer100GramsOverride)} на 100 г.";

    public string Title { get; }

    public ObservableCollection<CookingIngredientItemViewModel> Ingredients { get; } = [];

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public bool HasValidationErrors => ValidationMessages.Count > 0;

    public bool HasIngredients => Ingredients.Count > 0;

    public CookingSessionEditorViewModel(
        CookingSessionService cookingSessionService,
        CookingNutritionLlmService cookingNutritionLlmService,
        ProductCatalogService productCatalogService,
        FridgeInventoryService fridgeInventoryService,
        CookingSessionDraft draft,
        bool isNew,
        Action onSaved,
        Action onCancelled
    ) {
        ArgumentNullException.ThrowIfNull(cookingSessionService);
        ArgumentNullException.ThrowIfNull(cookingNutritionLlmService);
        ArgumentNullException.ThrowIfNull(productCatalogService);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(onSaved);
        ArgumentNullException.ThrowIfNull(onCancelled);
        ArgumentNullException.ThrowIfNull(fridgeInventoryService);

        this.cookingSessionService = cookingSessionService;
        this.cookingNutritionLlmService = cookingNutritionLlmService;
        this.productCatalogService = productCatalogService;
        this.fridgeInventoryService = fridgeInventoryService;

        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        sessionId = draft.Id;

        ingredients = [
            .. draft.Ingredients,
        ];
        nutritionPer100GramsOverride = draft.NutritionPer100GramsOverride;

        Title = isNew ? "Новое приготовление" : "Редактирование приготовления";

        name = draft.Name;
        outputWeightG = draft.OutputWeightG;
        note = draft.Note;

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

    partial void OnNameChanged(string value) {
        InvalidateLlmNutrition();
    }

    partial void OnNoteChanged(string? value) {
        InvalidateLlmNutrition();
    }

    partial void OnOutputWeightGChanged(decimal value) {
        InvalidateLlmNutrition();
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

        InvalidateLlmNutrition();
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

        InvalidateLlmNutrition();
        UpdatePreview();
    }

    [RelayCommand]
    private void PrepareLlmNutrition() {
        var draft = CreateDraft();

        if(string.IsNullOrWhiteSpace(draft.Name)) {
            LlmActionSummary = "Сначала укажите название блюда.";
            IsLlmNutritionPanelVisible = true;
            return;
        }

        if(draft.Ingredients.Count == 0
            || draft.Ingredients.Any(ingredient => ingredient.Quantity.Value <= 0m)
            || draft.OutputWeightG <= 0m
        ) {
            LlmActionSummary = "Для запроса нужны ингредиенты с корректным количеством и вес готового блюда больше 0 г.";
            IsLlmNutritionPanelVisible = true;
            return;
        }

        LlmRequestText = cookingNutritionLlmService.ExportRequest(draft);
        LlmResponseInstructions = cookingNutritionLlmService.CreateResponseInstructions(draft);
        LlmResponseText = string.Empty;
        LlmActionSummary = "Запрос подготовлен. Передайте JSON и инструкцию LLM, затем вставьте ответ ниже.";
        IsLlmNutritionPanelVisible = true;
    }

    [RelayCommand]
    private void ApplyLlmNutrition() {
        var result = cookingNutritionLlmService.ParseResponse(
            LlmResponseText,
            CreateDraft()
        );

        if(!result.IsSuccess) {
            LlmActionSummary = FormatLlmErrors(result.Errors);
            return;
        }

        nutritionPer100GramsOverride = result.NutritionPer100Grams;
        LlmActionSummary = result.Note is null
            ? "Оценка КБЖУ применена. Сохраните приготовление, чтобы она сохранилась."
            : $"Оценка КБЖУ применена. {result.Note}";
        OnPropertyChanged(nameof(HasNutritionOverride));
        OnPropertyChanged(nameof(NutritionOverrideSummary));
        UpdatePreview();
    }

    [RelayCommand]
    private void ClearNutritionOverride() {
        if(nutritionPer100GramsOverride is null) {
            return;
        }

        nutritionPer100GramsOverride = null;
        LlmActionSummary = "Оценка КБЖУ удалена. Используется расчёт по КБЖУ ингредиентов.";
        OnPropertyChanged(nameof(HasNutritionOverride));
        OnPropertyChanged(nameof(NutritionOverrideSummary));
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
            Note: Note,
            NutritionPer100GramsOverride: nutritionPer100GramsOverride
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

        InvalidateLlmNutrition();
        UpdatePreview();
    }

    private void RemoveIngredient(Guid id) {
        ingredients.RemoveAll(ingredient => ingredient.Id == id);

        var item = Ingredients.FirstOrDefault(ingredient => ingredient.Id == id);

        if(item is not null) {
            Ingredients.Remove(item);
        }

        OnPropertyChanged(nameof(HasIngredients));

        InvalidateLlmNutrition();
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
            CookingSessionValidationError.InvalidNutritionOverride => "Оценка КБЖУ готового блюда некорректна.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error,
                null
            )
        };
    }

    private void InvalidateLlmNutrition() {
        var hadOverride = nutritionPer100GramsOverride is not null;
        var hadExchange = !string.IsNullOrEmpty(LlmRequestText)
            || !string.IsNullOrEmpty(LlmResponseText);

        nutritionPer100GramsOverride = null;
        LlmRequestText = string.Empty;
        LlmResponseInstructions = string.Empty;
        LlmResponseText = string.Empty;

        if(hadOverride || hadExchange) {
            LlmActionSummary = "Состав или выход блюда изменён. Подготовьте новый запрос для LLM.";
        }

        if(hadOverride) {
            OnPropertyChanged(nameof(HasNutritionOverride));
            OnPropertyChanged(nameof(NutritionOverrideSummary));
        }
    }

    private static string FormatLlmErrors(IReadOnlyList<CookingNutritionLlmParseError> errors) {
        return string.Join(
            " ",
            errors.Select(error => error.Code switch {
                CookingNutritionLlmParseErrorCode.InvalidJson => "Ответ не является корректным JSON.",
                CookingNutritionLlmParseErrorCode.UnsupportedProtocol => "Ответ использует неподдерживаемый протокол.",
                CookingNutritionLlmParseErrorCode.SessionMismatch => "Ответ относится к другому приготовлению.",
                CookingNutritionLlmParseErrorCode.RequestMismatch => "Ответ относится к устаревшей версии состава или веса блюда.",
                CookingNutritionLlmParseErrorCode.MissingNutrition => "В ответе отсутствует nutritionPer100Grams.",
                CookingNutritionLlmParseErrorCode.InvalidNutrition => "Все значения КБЖУ должны быть числами не меньше 0.",
                _ => "Ответ LLM не удалось применить."
            })
        );
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
