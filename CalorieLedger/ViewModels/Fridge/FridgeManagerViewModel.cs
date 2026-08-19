using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.MealPlanning;
using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CalorieLedger.ViewModels.Fridge;

public partial class FridgeManagerViewModel:ViewModelBase {
    private readonly FridgeInventoryService fridgeInventoryService;
    private readonly ProductCatalogService productCatalogService;
    private readonly FridgeMealPlanningExportService fridgeMealPlanningExportService;
    private readonly MealPlanResponseParser mealPlanResponseParser;
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

    [ObservableProperty]
    private string mealPlanningExportText = string.Empty;

    [ObservableProperty]
    private bool isMealPlanningExportVisible;

    [ObservableProperty]
    private string mealPlanningResponseInstructions = string.Empty;

    [ObservableProperty]
    private string mealPlanningResponseText = string.Empty;

    [ObservableProperty]
    private string mealPlanningResponseStatus = string.Empty;

    [ObservableProperty]
    private string mealPlanningPreviewText = string.Empty;

    [ObservableProperty]
    private bool isMealPlanningPreviewVisible;

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
        fridgeMealPlanningExportService = new FridgeMealPlanningExportService(fridgeInventoryService);
        mealPlanResponseParser = new MealPlanResponseParser();

        this.currentDate = currentDate;
        MealPlanningResponseInstructions = mealPlanResponseParser.CreateResponseInstructions(currentDate);

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
    private void ExportForMealPlanning() {
        RefreshMealPlanningExport();
        IsMealPlanningExportVisible = true;
    }

    [RelayCommand]
    private void HideMealPlanningExport() {
        IsMealPlanningExportVisible = false;
    }

    [RelayCommand]
    private void ParseMealPlanningResponse() {
        var result = mealPlanResponseParser.Parse(MealPlanningResponseText);

        if(!result.IsSuccess) {
            MealPlanningResponseStatus = FormatMealPlanErrors(result.Errors);
            MealPlanningPreviewText = string.Empty;
            IsMealPlanningPreviewVisible = false;

            return;
        }

        var plan = result.Plan!;
        var mealCount = plan.Days.Sum(day => day.Meals.Count);
        var itemCount = plan.Days.Sum(day => day.Meals.Sum(meal => meal.Items.Count));

        MealPlanningResponseStatus = $"План распознан: {plan.Days.Count} дн., {mealCount} приёмов пищи, {itemCount} позиций.";
        MealPlanningPreviewText = FormatMealPlanPreview(plan);
        IsMealPlanningPreviewVisible = true;
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

        if(IsMealPlanningExportVisible) {
            RefreshMealPlanningExport();
        }
    }

    private void DeleteItem(Guid id) {
        fridgeInventoryService.Delete(id);

        RefreshItems();
    }

    private void RefreshMealPlanningExport() {
        MealPlanningExportText = fridgeMealPlanningExportService.Export(currentDate);
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

    private static string FormatMealPlanErrors(IReadOnlyList<MealPlanParseError> errors) {
        var formattedErrors = errors
            .Take(5)
            .Select(error => $"{error.Path}: {FormatMealPlanError(error.Code)}")
            .ToArray();

        var suffix = errors.Count > formattedErrors.Length
            ? $" Ещё ошибок: {errors.Count - formattedErrors.Length}."
            : string.Empty;

        return $"Не удалось разобрать план. {string.Join(" ", formattedErrors)}{suffix}";
    }

    private static string FormatMealPlanError(MealPlanParseErrorCode error) {
        return error switch {
            MealPlanParseErrorCode.InvalidJson => "ответ не является корректным JSON ожидаемого формата.",
            MealPlanParseErrorCode.UnsupportedProtocol => $"ожидается protocol {MealPlanResponseParser.Protocol}.",
            MealPlanParseErrorCode.MissingDays => "нужен непустой массив days.",
            MealPlanParseErrorCode.MissingDate => "не указана дата.",
            MealPlanParseErrorCode.DuplicateDate => "эта дата уже присутствует в плане.",
            MealPlanParseErrorCode.MissingMeals => "нужен хотя бы один приём пищи.",
            MealPlanParseErrorCode.MissingMealName => "не указано название приёма пищи.",
            MealPlanParseErrorCode.UnsupportedMealRole => "неподдерживаемая роль приёма пищи.",
            MealPlanParseErrorCode.MissingItems => "нужна хотя бы одна позиция.",
            MealPlanParseErrorCode.MissingItemName => "не указано название позиции.",
            MealPlanParseErrorCode.InvalidQuantity => "количество должно быть больше 0.",
            MealPlanParseErrorCode.UnsupportedQuantityUnit => "неподдерживаемая единица количества.",
            MealPlanParseErrorCode.InvalidNutrition => "КБЖУ не могут быть отрицательными.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error,
                null
            )
        };
    }

    private static string FormatMealPlanPreview(MealPlan plan) {
        var builder = new StringBuilder();
        var culture = CultureInfo.GetCultureInfo("ru-RU");

        foreach(var day in plan.Days) {
            if(builder.Length > 0) {
                builder.AppendLine();
            }

            builder.AppendLine(
                $"{day.Date.ToString("dddd, dd.MM.yyyy", culture)}"
            );

            foreach(var meal in day.Meals) {
                builder.Append("  ");
                builder.Append(FormatMealRole(meal.Role));

                if(meal.Time is TimeOnly time) {
                    builder.Append(" · ");
                    builder.Append(time.ToString("HH:mm", culture));
                }

                if(!string.Equals(
                    meal.Name,
                    FormatMealRole(meal.Role),
                    StringComparison.OrdinalIgnoreCase
                )) {
                    builder.Append(" · ");
                    builder.Append(meal.Name);
                }

                builder.AppendLine();

                foreach(var item in meal.Items) {
                    builder.Append("    • ");
                    builder.Append(item.Name);
                    builder.Append(" — ");
                    builder.Append(FormatQuantity(item.Quantity));

                    var nutrition = FormatNutrition(item.Nutrition);

                    if(nutrition.Length > 0) {
                        builder.Append(" · ");
                        builder.Append(nutrition);
                    }

                    if(item.FridgeItemId is not null) {
                        builder.Append(" · из холодильника");
                    }

                    builder.AppendLine();
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatMealRole(MealGroupRole role) {
        return role switch {
            MealGroupRole.Breakfast => "Завтрак",
            MealGroupRole.Lunch => "Обед",
            MealGroupRole.Dinner => "Ужин",
            MealGroupRole.Snack => "Перекус",
            MealGroupRole.Custom => "Приём пищи",
            _ => throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                null
            )
        };
    }

    private static string FormatQuantity(FoodQuantity quantity) {
        var unit = quantity.Unit switch {
            FoodUnit.Gram => "г",
            FoodUnit.Milliliter => "мл",
            FoodUnit.Piece => "шт.",
            FoodUnit.Portion => "порц.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity.Unit,
                null
            )
        };

        return $"{quantity.Value:0.##} {unit}";
    }

    private static string FormatNutrition(NutritionTotals nutrition) {
        var parts = new List<string>();

        if(nutrition.CaloriesKcal is decimal calories) {
            parts.Add($"{calories:0.##} ккал");
        }

        if(nutrition.ProteinG is decimal protein) {
            parts.Add($"Б {protein:0.##} г");
        }

        if(nutrition.FatG is decimal fat) {
            parts.Add($"Ж {fat:0.##} г");
        }

        if(nutrition.CarbsG is decimal carbs) {
            parts.Add($"У {carbs:0.##} г");
        }

        return string.Join(" · ", parts);
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
