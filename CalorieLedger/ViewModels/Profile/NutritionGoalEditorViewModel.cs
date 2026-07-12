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
    private decimal? desiredWeightChangeKgPerWeek;

    [ObservableProperty]
    private decimal? energyBalancePercent;

    [ObservableProperty]
    private decimal? stopAtBodyFatPercent;

    [ObservableProperty]
    private MassGainIntent? massGainIntent;

    [ObservableProperty]
    private string statusSummary = string.Empty;

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
        DesiredWeightChangeKgPerWeek =
            draft.DesiredWeightChangeKgPerWeek;
        EnergyBalancePercent = draft.EnergyBalancePercent;
        StopAtBodyFatPercent = draft.StopAtBodyFatPercent;
        MassGainIntent = draft.MassGainIntent;
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

    partial void OnGoalTypeChanged(
        WeightGoalType value) {
        OnPropertyChanged(nameof(IsMaintenance));
        OnPropertyChanged(nameof(IsWeightLoss));
        OnPropertyChanged(nameof(IsWeightGain));
        OnPropertyChanged(nameof(CanEditEnergyStrategy));
        OnPropertyChanged(nameof(CanEditMassGainOptions));
    }

    private NutritionGoalDraft CreateDraft() {
        var normalizedEnergyBalancePercent =
            GoalType == WeightGoalType.Maintain
                ? 0m
                : EnergyBalancePercent;

        var normalizedWeightChange =
            GoalType == WeightGoalType.Maintain
                ? null
                : DesiredWeightChangeKgPerWeek;

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
            TargetBodyFatPercent:
                TargetBodyFatPercent,
            TargetMuscleMassKg:
                TargetMuscleMassKg,
            TargetMusclePercent:
                TargetMusclePercent,
            DesiredWeightChangeKgPerWeek:
                normalizedWeightChange,
            EnergyBalancePercent:
                normalizedEnergyBalancePercent,
            StopAtBodyFatPercent:
                normalizedStopAtBodyFat,
            MassGainIntent:
                normalizedMassGainIntent);
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

            NutritionGoalValidationError.InvalidDesiredWeightChange =>
                "Желаемое изменение веса должно быть больше нуля.",

            NutritionGoalValidationError.InvalidEnergyBalancePercent =>
                "Некорректный процент дефицита или профицита.",

            NutritionGoalValidationError.MissingEnergyStrategy =>
                "Укажите процент дефицита/профицита либо изменение веса в неделю.",

            NutritionGoalValidationError.ConflictingEnergyStrategies =>
                "Нельзя одновременно указывать процент баланса и изменение веса в неделю.",

            NutritionGoalValidationError.WeightLossRequiresDeficit =>
                "Для снижения веса требуется отрицательный процент энергетического баланса.",

            NutritionGoalValidationError.WeightGainRequiresSurplus =>
                "Для набора массы требуется положительный процент энергетического баланса.",

            NutritionGoalValidationError.MaintenanceRequiresNeutralEnergyBalance =>
                "Для поддержания энергетический баланс должен быть равен 0%.",

            NutritionGoalValidationError.WeightChangeNotAllowedForMaintenance =>
                "Для поддержания нельзя задавать изменение веса в неделю.",

            NutritionGoalValidationError.InvalidStopBodyFatPercent =>
                "Предел процента жира должен находиться между 0 и 100%.",

            NutritionGoalValidationError.StopBodyFatOnlyForWeightGain =>
                "Предел процента жира применяется только при наборе массы.",

            NutritionGoalValidationError.MassGainIntentOnlyForWeightGain =>
                "Тип набора массы применяется только к цели набора веса.",

            _ => $"Неизвестная ошибка проверки: {error}."
        };
    }
}