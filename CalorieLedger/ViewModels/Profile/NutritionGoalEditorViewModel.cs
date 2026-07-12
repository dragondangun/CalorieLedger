using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Profile;

public partial class NutritionGoalEditorViewModel:ViewModelBase {
    private readonly NutritionGoalEditorService editorService;
    private readonly Action onSaved;
    private readonly Action onCancelled;

    [ObservableProperty]
    private WeightGoalType goalType;

    [ObservableProperty]
    private decimal? targetWeightKg;

    [ObservableProperty]
    private decimal? targetBodyFatPercent;

    [ObservableProperty]
    private decimal? targetMuscleMassKg;

    [ObservableProperty]
    private decimal? targetMusclePercent;

    [ObservableProperty]
    private EnergyStrategyMode strategyMode;

    [ObservableProperty]
    private decimal? strategyValue;

    [ObservableProperty]
    private string strategyPreviewSummary = string.Empty;

    [ObservableProperty]
    private decimal? stopAtBodyFatPercent;

    [ObservableProperty]
    private MassGainIntent? massGainIntent;

    [ObservableProperty]
    private string statusSummary = string.Empty;

    public IReadOnlyList<EnergyStrategyMode> StrategyModes { get; } = Enum.GetValues<EnergyStrategyMode>();

    public decimal StrategyValueMaximum => StrategyMode switch
    {
        EnergyStrategyMode.BalancePercent => 99.9m,
        EnergyStrategyMode.WeightChangePerWeek => 5m,

        _ => throw new ArgumentOutOfRangeException(
            nameof(StrategyMode),
            StrategyMode,
            null)
    };

    public decimal StrategyValueIncrement => StrategyMode switch
    {
        EnergyStrategyMode.BalancePercent => 0.5m,
        EnergyStrategyMode.WeightChangePerWeek => 0.05m,

        _ => throw new ArgumentOutOfRangeException(
            nameof(StrategyMode),
            StrategyMode,
            null)
    };

    public string StrategyValueLabel => GoalType switch
    {
        WeightGoalType.Maintain =>
            "Энергетический баланс, %",

        WeightGoalType.LoseWeight =>
            StrategyMode switch
            {
                EnergyStrategyMode.BalancePercent =>
                    "Величина дефицита, %",

                EnergyStrategyMode.WeightChangePerWeek =>
                    "Снижение веса, кг/нед.",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(StrategyMode),
                    StrategyMode,
                    null)
            },

        WeightGoalType.GainWeight =>
            StrategyMode switch
            {
                EnergyStrategyMode.BalancePercent =>
                    "Величина профицита, %",

                EnergyStrategyMode.WeightChangePerWeek =>
                    "Набор веса, кг/нед.",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(StrategyMode),
                    StrategyMode,
                    null)
            },

        _ => throw new ArgumentOutOfRangeException(
            nameof(GoalType),
            GoalType,
            null)
    };

    public NutritionGoalEditorViewModel(
        NutritionGoalEditorService editorService,
        NutritionGoalDraft draft,
        Action onSaved,
        Action onCancelled) {
        this.editorService = editorService;
        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        GoalType = draft.GoalType;
        TargetWeightKg = draft.TargetWeightKg;
        TargetBodyFatPercent = draft.TargetBodyFatPercent;
        TargetMuscleMassKg = draft.TargetMuscleMassKg;
        TargetMusclePercent = draft.TargetMusclePercent;
        StrategyMode = draft.StrategyMode;
        StrategyValue = draft.StrategyValue;
        StopAtBodyFatPercent = draft.StopAtBodyFatPercent;
        MassGainIntent = draft.MassGainIntent;
        UpdateStrategyPreview();
    }

    partial void OnStrategyModeChanged(
    EnergyStrategyMode value) {
        OnPropertyChanged(
            nameof(StrategyValueLabel));

        OnPropertyChanged(
            nameof(StrategyValueMaximum));

        OnPropertyChanged(
            nameof(StrategyValueIncrement));

        UpdateStrategyPreview();
    }

    partial void OnStrategyValueChanged(decimal? value) {
        UpdateStrategyPreview();
    }

    public IReadOnlyList<WeightGoalType> GoalTypes { get; } =
        Enum.GetValues<WeightGoalType>();

    public IReadOnlyList<MassGainIntent> MassGainIntents { get; } =
        Enum.GetValues<MassGainIntent>();

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public bool HasValidationErrors =>
        ValidationMessages.Count > 0;

    public bool IsMaintenance =>
        GoalType == WeightGoalType.Maintain;

    public bool IsWeightLoss =>
        GoalType == WeightGoalType.LoseWeight;

    public bool IsWeightGain =>
        GoalType == WeightGoalType.GainWeight;

    public bool CanEditEnergyStrategy =>
        GoalType != WeightGoalType.Maintain;

    public bool CanEditMassGainOptions =>
        GoalType == WeightGoalType.GainWeight;

    [RelayCommand]
    private void Save() {
        ClearValidationMessages();

        var draft = CreateDraft();
        var result = editorService.Save(draft);

        if(!result.IsSuccess) {
            foreach(var error in result.Errors) {
                ValidationMessages.Add(
                    FormatValidationError(error));
            }

            StatusSummary =
                "Цель не сохранена. Исправьте указанные ошибки.";

            OnPropertyChanged(nameof(HasValidationErrors));
            return;
        }

        StatusSummary = "Цель успешно сохранена.";
        onSaved();
    }

    [RelayCommand]
    private void Cancel() {
        StatusSummary = "Изменения отменены.";
        onCancelled();
    }

    partial void OnGoalTypeChanged(WeightGoalType value) {
        OnPropertyChanged(nameof(IsMaintenance));
        OnPropertyChanged(nameof(IsWeightLoss));
        OnPropertyChanged(nameof(IsWeightGain));
        OnPropertyChanged(nameof(CanEditEnergyStrategy));
        OnPropertyChanged(nameof(CanEditMassGainOptions));
        OnPropertyChanged(nameof(StrategyValueLabel));

        if(value == WeightGoalType.Maintain) {
            StrategyMode =
                EnergyStrategyMode.BalancePercent;

            StrategyValue = 0m;
        }
        else if(StrategyValue == 0m) {
            StrategyValue = null;
        }

        UpdateStrategyPreview();
    }

    private NutritionGoalDraft CreateDraft() {
        var normalizedStrategyMode =
        GoalType == WeightGoalType.Maintain
            ? EnergyStrategyMode.BalancePercent
            : StrategyMode;

        var normalizedStrategyValue =
        GoalType == WeightGoalType.Maintain
            ? 0m
            : StrategyValue;

        var normalizedStopAtBodyFat =
        GoalType == WeightGoalType.GainWeight
            ? StopAtBodyFatPercent
            : null;

        var normalizedMassGainIntent =
        GoalType == WeightGoalType.GainWeight
            ? MassGainIntent
            : null;

        return new NutritionGoalDraft(
            GoalType: GoalType,
            TargetWeightKg: TargetWeightKg,
            TargetBodyFatPercent: TargetBodyFatPercent,
            TargetMuscleMassKg: TargetMuscleMassKg,
            TargetMusclePercent: TargetMusclePercent,
            StrategyMode: normalizedStrategyMode,
            StrategyValue: normalizedStrategyValue,
            StopAtBodyFatPercent: normalizedStopAtBodyFat,
            MassGainIntent: normalizedMassGainIntent);
    }

    private void UpdateStrategyPreview() {
        var preview = editorService.CalculateStrategyPreview(
            CreateDraft());

        if(preview is null) {
            StrategyPreviewSummary = GoalType == WeightGoalType.Maintain
                ? "Поддержание: калорийность без дефицита или профицита."
                : StrategyMode == EnergyStrategyMode.BalancePercent
                    ? "Введите процент больше 0 и меньше 100."
                    : "Введите изменение веса больше 0 кг/нед.";

            return;
        }

        StrategyPreviewSummary =
            $"Поддержание: " +
            $"{preview.MaintenanceCaloriesKcal:0} ккал/день. " +
            $"Целевая калорийность: " +
            $"{preview.TargetCaloriesKcal:0} ккал/день. " +
            $"Баланс: " +
            $"{FormatSigned(preview.EnergyBalancePercent)}%. " +
            $"Расчётное изменение веса: " +
            $"{FormatSigned(preview.PredictedWeightChangeKgPerWeek)} " +
            "кг/нед.";
    }

    private static string FormatSigned(
        decimal value) {
        return value > 0m
            ? $"+{value:0.##}"
            : $"{value:0.##}";
    }

    private void ClearValidationMessages() {
        ValidationMessages.Clear();
        StatusSummary = string.Empty;

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    private static string FormatValidationError(
    NutritionGoalValidationError error) {
        return error switch
        {
            NutritionGoalValidationError.InvalidTargetWeight =>
                "Целевой вес должен быть больше нуля.",

            NutritionGoalValidationError.InvalidTargetBodyFatPercent =>
                "Целевой процент жира должен находиться между 0 и 100%.",

            NutritionGoalValidationError.InvalidTargetMuscleMass =>
                "Целевая мышечная масса должна быть больше нуля.",

            NutritionGoalValidationError.InvalidTargetMusclePercent =>
                "Целевой процент мышц должен находиться между 0 и 100%.",

            NutritionGoalValidationError.MissingEnergyStrategy =>
                "Укажите величину энергетической стратегии.",

            NutritionGoalValidationError.MaintenanceRequiresNeutralEnergyBalance =>
                "Для поддержания величина энергетической стратегии должна быть равна нулю.",

            NutritionGoalValidationError.InvalidStopBodyFatPercent =>
                "Предел процента жира должен находиться между 0 и 100%.",

            NutritionGoalValidationError.StopBodyFatOnlyForWeightGain =>
                "Предел процента жира применяется только при наборе массы.",

            NutritionGoalValidationError.MassGainIntentOnlyForWeightGain =>
                "Тип набора массы применяется только к цели набора веса.",

            NutritionGoalValidationError.InvalidEnergyStrategyValue =>
                "Величина энергетической стратегии некорректна.",

            _ =>
                $"Неизвестная ошибка проверки: {error}."
        };
    }
}