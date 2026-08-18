using CalorieLedger.Application.Activities;
using CalorieLedger.Domain.Activities;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CalorieLedger.ViewModels.Activities;

public partial class ActivityEditorViewModel:ViewModelBase {
    private readonly ActivityEditorService editorService;
    private readonly Guid activityId;
    private readonly DateOnly activityDate;
    private readonly DateOnly currentDate;
    private readonly Action onSaved;
    private readonly Action onCancelled;
    private readonly ActivityEnergySuggestionService energySuggestionService;
    private ActivityEnergyCalculation? energyCalculation;
    private bool isApplyingEstimate;
    private readonly ActivityPresetCatalogService activityPresetCatalogService;
    private bool isRefreshingPresets;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EstimateCaloriesCommand))]
    private ActivityPreset? selectedPreset;

    [ObservableProperty]
    private string estimateSummary = string.Empty;

    public bool HasEstimateSummary => !string.IsNullOrWhiteSpace(EstimateSummary);

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private decimal? burnedCaloriesKcal;

    [ObservableProperty]
    private TimeSpan? startedAtTime;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EstimateCaloriesCommand))]
    private decimal? durationMinutes;

    [ObservableProperty]
    private string? note;

    public string Title { get; }

    public ObservableCollection<string> ValidationMessages { get; } = [];
    public IReadOnlyList<ActivityPreset> ActivityPresets { get; private set; } = [];

    [ObservableProperty]
    private ActivityPresetManagerViewModel? presetManager;

    public bool IsPresetManagerOpen => PresetManager is not null;

    public bool HasValidationErrors => ValidationMessages.Count > 0;

    public ActivityEditorViewModel(
        ActivityEditorService editorService,
        ActivityDraft draft,
        DateOnly currentDate,
        bool isNew,
        Action onSaved,
        Action onCancelled,
        ActivityPresetCatalogService activityPresetCatalogService,
        ActivityEnergySuggestionService energySuggestionService
    ) {
        ArgumentNullException.ThrowIfNull(editorService);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(onSaved);
        ArgumentNullException.ThrowIfNull(onCancelled);
        ArgumentNullException.ThrowIfNull(energySuggestionService);
        ArgumentNullException.ThrowIfNull(activityPresetCatalogService);

        this.activityPresetCatalogService = activityPresetCatalogService;
        this.energySuggestionService = energySuggestionService;

        energyCalculation = draft.EnergyCalculation;
        RefreshActivityPresets(draft.EnergyCalculation?.PresetCode);
        EstimateSummary = FormatEstimateSummary(energyCalculation);

        this.editorService = editorService;
        this.currentDate = currentDate;
        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        activityId = draft.Id;

        activityDate = draft.Date;

        Title = isNew ? "Добавление активности" : "Редактирование активности";

        Name = draft.Name;

        BurnedCaloriesKcal = draft.BurnedCaloriesKcal;

        StartedAtTime = draft.StartedAt?.ToTimeSpan();

        DurationMinutes = draft.Duration is null
            ? null
            : (decimal)draft.Duration.Value.TotalMinutes;

        Note = draft.Note;
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

    [RelayCommand(CanExecute = nameof(CanEstimateCalories))]
    private void EstimateCalories() {
        if(SelectedPreset is null || DurationMinutes is not > 0m) {
            return;
        }

        var suggestion = energySuggestionService.Estimate(
            activityDate,
            SelectedPreset,
            DurationMinutes.Value
        );

        if(suggestion is null) {
            EstimateSummary = "Для расчёта нужно измерение веса на эту дату или раньше.";
            OnPropertyChanged(nameof(HasEstimateSummary));
            return;
        }

        isApplyingEstimate = true;
        BurnedCaloriesKcal = suggestion.BurnedCaloriesKcal;
        isApplyingEstimate = false;

        energyCalculation = suggestion.Calculation;
        EstimateSummary = FormatEstimateSummary(energyCalculation);
        OnPropertyChanged(nameof(HasEstimateSummary));
    }

    [RelayCommand]
    private void ManagePresets() {
        PresetManager = new ActivityPresetManagerViewModel(
            activityPresetCatalogService,
            RefreshActivityPresets,
            ClosePresetManager
        );
    }

    private void ClosePresetManager() {
        PresetManager = null;
    }

    private void RefreshActivityPresets() {
        RefreshActivityPresets(SelectedPreset?.Code);
    }

    private void RefreshActivityPresets(string? selectedCode) {
        isRefreshingPresets = true;

        ActivityPresets = activityPresetCatalogService.GetAll();
        OnPropertyChanged(nameof(ActivityPresets));

        SelectedPreset = ActivityPresets.FirstOrDefault(preset => preset.Code == selectedCode);
        isRefreshingPresets = false;
    }

    partial void OnPresetManagerChanged(ActivityPresetManagerViewModel? value) {
        OnPropertyChanged(nameof(IsPresetManagerOpen));
    }

    private ActivityDraft CreateDraft() {
        return new ActivityDraft(
            Id: activityId,
            Date: activityDate,
            Name: Name,
            BurnedCaloriesKcal: BurnedCaloriesKcal,
            StartedAt: StartedAtTime is null
                ? null
                : TimeOnly.FromTimeSpan(StartedAtTime.Value),
            Duration: DurationMinutes is null
                ? null
                : TimeSpan.FromMinutes((double)DurationMinutes.Value),
            Note: Note,
            EnergyCalculation: energyCalculation
        );
    }

    private void ClearValidationMessages() {
        ValidationMessages.Clear();

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    private static string FormatValidationError(ActivityValidationError error) {
        return error switch {
            ActivityValidationError.MissingId => "Не удалось определить запись активности.",
            ActivityValidationError.FutureDate => "Дата активности не может быть в будущем.",
            ActivityValidationError.MissingName => "Введите название активности.",
            ActivityValidationError.InvalidBurnedCalories => "Расход энергии должен быть больше 0 ккал.",
            ActivityValidationError.InvalidDuration => "Продолжительность должна быть больше 0 минут.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error,
                null
            )
        };
    }

    partial void OnBurnedCaloriesKcalChanged(decimal? value) {
        if(!isApplyingEstimate) {
            ClearEnergyCalculation();
        }
    }

    partial void OnDurationMinutesChanged(decimal? value) {
        EstimateCaloriesCommand.NotifyCanExecuteChanged();

        if(energyCalculation is not null) {
            ClearEnergyCalculation();
        }
    }

    partial void OnSelectedPresetChanged(ActivityPreset? value) {
        if(isRefreshingPresets) {
            return;
        }

        EstimateCaloriesCommand.NotifyCanExecuteChanged();

        if(energyCalculation is not null
            && value?.Code != energyCalculation.PresetCode) {
            ClearEnergyCalculation();
        }
    }

    private bool CanEstimateCalories() {
        return SelectedPreset is not null && DurationMinutes is > 0m;
    }

    private void ClearEnergyCalculation() {
        energyCalculation = null;
        EstimateSummary = string.Empty;
        OnPropertyChanged(nameof(HasEstimateSummary));
    }

    private static string FormatEstimateSummary(ActivityEnergyCalculation? calculation) {
        if(calculation is null) {
            return string.Empty;
        }

        return $"Оценка: {calculation.MetValue:0.#} MET · "
            + $"{calculation.WeightKg:0.#} кг · {calculation.DurationMinutes:0} мин";
    }
}
