using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace CalorieLedger.ViewModels.Meals;

public partial class FoodLogEditorViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly FoodLogEditorService editorService;
    private readonly DateOnly currentDate;
    private readonly Guid foodLogId;
    private readonly DateOnly foodLogDate;
    private readonly Action onSaved;
    private readonly Action onCancelled;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private MealGroupRole mealRole;

    [ObservableProperty]
    private SelectionOption<MealGroupRole>? selectedMealRoleOption;

    [ObservableProperty]
    private decimal? quantityValue;

    [ObservableProperty]
    private FoodUnit quantityUnit;

    [ObservableProperty]
    private SelectionOption<FoodUnit>? selectedQuantityUnitOption;

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

    [ObservableProperty]
    private bool isApproximate;

    [ObservableProperty]
    private string? note;

    [ObservableProperty]
    private string nutritionPreviewSummary = string.Empty;

    [ObservableProperty]
    private FoodLogEditorViewModel? foodLogEditor;

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public bool HasValidationErrors => ValidationMessages.Count > 0;

    public IReadOnlyList<SelectionOption<MealGroupRole>> MealRoleOptions { get; } = [
        new(
            MealGroupRole.Breakfast,
            "Завтрак"
        ),
        new(
            MealGroupRole.Lunch,
            "Обед"
        ),
        new(
            MealGroupRole.Dinner,
            "Ужин"
        ),
        new(
            MealGroupRole.Snack,
            "Перекусы"
        ),
        new(
            MealGroupRole.Custom,
            "Другое"
        ),
    ];

    public IReadOnlyList<SelectionOption<FoodUnit>> QuantityUnitOptions { get; } = [
        new(
            FoodUnit.Gram,
            "г"
        ),
        new(
            FoodUnit.Milliliter,
            "мл"
        ),
        new(
            FoodUnit.Piece,
            "шт."
        ),
        new(
            FoodUnit.Portion,
            "порция"
        ),
    ];

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
        new(
            NutritionBasis.Total,
            "На всё указанное количество"
        ),
    ];

    public FoodLogEditorViewModel(
        FoodLogEditorService editorService,
        FoodLogDraft draft,
        DateOnly currentDate,
        Action onSaved,
        Action onCancelled
    ) {
        ArgumentNullException.ThrowIfNull(editorService);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(onSaved);
        ArgumentNullException.ThrowIfNull(onCancelled);

        this.editorService = editorService;
        this.currentDate = currentDate;
        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        foodLogId = draft.Id;
        foodLogDate = draft.Date;
        Name = draft.Name;
        MealRole = draft.MealRole;
        QuantityValue = draft.QuantityValue;
        QuantityUnit = draft.QuantityUnit;
        NutritionBasis = draft.NutritionBasis;
        CaloriesKcal = draft.CaloriesKcal;
        ProteinG = draft.ProteinG;
        FatG = draft.FatG;
        CarbsG = draft.CarbsG;
        IsApproximate = draft.IsApproximate;
        Note = draft.Note;

        SelectedMealRoleOption = MealRoleOptions.First(
            option => option.Value == MealRole
        );

        SelectedQuantityUnitOption = QuantityUnitOptions.First(
            option => option.Value == QuantityUnit
        );

        SelectedNutritionBasisOption = NutritionBasisOptions.First(
            option => option.Value == NutritionBasis
        );

        UpdatePreview();
    }

    partial void OnMealRoleChanged(MealGroupRole value) {
        var option = MealRoleOptions.First(
            existing => existing.Value == value
        );

        if(SelectedMealRoleOption != option) {
            SelectedMealRoleOption = option;
        }
    }

    partial void OnSelectedMealRoleOptionChanged(SelectionOption<MealGroupRole>? value) {
        if(value is not null
            && MealRole != value.Value) {
            MealRole = value.Value;
        }
    }

    partial void OnQuantityUnitChanged(FoodUnit value) {
        var option = QuantityUnitOptions.First(
            existing => existing.Value == value
        );

        if(SelectedQuantityUnitOption != option) {
            SelectedQuantityUnitOption = option;
        }

        UpdatePreview();
    }

    partial void OnSelectedQuantityUnitOptionChanged(SelectionOption<FoodUnit>? value) {
        if(value is not null
            && QuantityUnit != value.Value) {
            QuantityUnit = value.Value;
        }
    }

    partial void OnNutritionBasisChanged(NutritionBasis value) {
        var option = NutritionBasisOptions.First(
            existing => existing.Value == value
        );

        if(SelectedNutritionBasisOption != option) {
            SelectedNutritionBasisOption = option;
        }

        UpdatePreview();
    }

    partial void OnSelectedNutritionBasisOptionChanged(SelectionOption<NutritionBasis>? value) {
        if(value is not null
            && NutritionBasis != value.Value) {
            NutritionBasis = value.Value;
        }
    }

    partial void OnQuantityValueChanged(decimal? value) {
        UpdatePreview();
    }

    partial void OnCaloriesKcalChanged(decimal? value) {
        UpdatePreview();
    }

    partial void OnProteinGChanged(decimal? value) {
        UpdatePreview();
    }

    partial void OnFatGChanged(decimal? value) {
        UpdatePreview();
    }

    partial void OnCarbsGChanged(decimal? value) {
        UpdatePreview();
    }

    [RelayCommand]
    private void Save() {
        ClearValidationMessages();

        var result = editorService.Save(
            CreateDraft(),
            currentDate
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

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    [RelayCommand]
    private void Cancel() {
        onCancelled();
    }

    private FoodLogDraft CreateDraft() {
        return new FoodLogDraft(
            Id: foodLogId,
            Date: foodLogDate,
            Name: Name,
            MealRole: MealRole,
            QuantityValue: QuantityValue,
            QuantityUnit: QuantityUnit,
            NutritionBasis: NutritionBasis,
            CaloriesKcal: CaloriesKcal,
            ProteinG: ProteinG,
            FatG: FatG,
            CarbsG: CarbsG,
            IsApproximate: IsApproximate,
            Note: Note
        );
    }

    private void UpdatePreview() {
        if(QuantityValue is not > 0m) {
            NutritionPreviewSummary = "Введите количество, чтобы рассчитать итоговое КБЖУ.";
            return;
        }

        var preview = editorService.CalculatePreview(CreateDraft());

        if(preview is null) {
            NutritionPreviewSummary = "Единица количества не соответствует способу задания КБЖУ.";

            return;
        }

        NutritionPreviewSummary = $"Итого: {FormatValue(preview.CaloriesKcal)} ккал · Б: {FormatValue(preview.ProteinG)} г · Ж: {FormatValue(preview.FatG)} г · У: {FormatValue(preview.CarbsG)} г";
    }

    private void ClearValidationMessages() {
        ValidationMessages.Clear();

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    private static string FormatValidationError(FoodLogValidationError error) {
        return error switch {
            FoodLogValidationError.MissingId =>
                "Не удалось определить запись еды.",

            FoodLogValidationError.FutureDate =>
                "Дата записи не может быть в будущем.",

            FoodLogValidationError.MissingName =>
                "Введите название продукта или блюда.",

            FoodLogValidationError.InvalidQuantity =>
                "Укажите количество больше 0.",

            FoodLogValidationError.IncompatibleNutritionBasis =>
                "Единица количества не соответствует способу задания КБЖУ.",

            FoodLogValidationError.InvalidCalories =>
                "Калорийность не может быть отрицательной.",

            FoodLogValidationError.InvalidProtein =>
                "Количество белка не может быть отрицательным.",

            FoodLogValidationError.InvalidFat =>
                "Количество жира не может быть отрицательным.",

            FoodLogValidationError.InvalidCarbs =>
                "Количество углеводов не может быть отрицательным.",

            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error,
                null
            )
        };
    }

    private static string FormatValue(decimal? value) {
        return value is null
            ? "—"
            : value.Value.ToString(
                "0.##",
                RussianCulture
            );
    }
}
