using CalorieLedger.Application.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class PlannedActivityEditorViewModel:ViewModelBase {
    private readonly PlannedActivityService service;
    private readonly Guid id;
    private readonly Action onSaved;
    private readonly Action onCancelled;

    [ObservableProperty]
    private DateOnly date;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private TimeSpan? plannedAtTime;

    [ObservableProperty]
    private decimal? durationMinutes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManualEnergyVisible))]
    private ActivityPreset? selectedPreset;

    [ObservableProperty]
    private decimal? manualBurnedCaloriesKcal;

    [ObservableProperty]
    private string? note;

    public IReadOnlyList<ActivityPreset> ActivityPresets { get; }
    public ObservableCollection<string> ValidationMessages { get; } = [];
    public bool HasValidationErrors => ValidationMessages.Count > 0;
    public bool IsManualEnergyVisible => SelectedPreset is null;
    public string Title { get; }

    public PlannedActivityEditorViewModel(
        PlannedActivityService service,
        ActivityPresetCatalogService presetCatalogService,
        PlannedActivityDraft draft,
        bool isNew,
        Action onSaved,
        Action onCancelled
    ) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(presetCatalogService);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(onSaved);
        ArgumentNullException.ThrowIfNull(onCancelled);

        this.service = service;
        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        id = draft.Id;
        Date = draft.Date;
        Name = draft.Name;
        PlannedAtTime = draft.PlannedAt?.ToTimeSpan();
        DurationMinutes = draft.Duration is null
            ? null
            : (decimal)draft.Duration.Value.TotalMinutes;
        ManualBurnedCaloriesKcal = draft.ManualBurnedCaloriesKcal;
        Note = draft.Note;
        Title = isNew ? "Новая запланированная активность" : "Редактирование плана";

        var presets = presetCatalogService.GetAll().ToList();
        SelectedPreset = presets.FirstOrDefault(preset => preset.Code == draft.PresetCode);

        if(SelectedPreset is null
            && draft.PresetCode is not null
            && draft.MetValue is not null) {
            SelectedPreset = new ActivityPreset(
                draft.PresetCode,
                $"{draft.Name} · сохранённый тип",
                draft.MetValue.Value
            );

            presets.Add(SelectedPreset);
        }

        ActivityPresets = presets;
    }

    [RelayCommand]
    private void UseManualEnergy() {
        SelectedPreset = null;
    }

    [RelayCommand]
    private void Save() {
        ValidationMessages.Clear();

        var result = service.Save(
            new PlannedActivityDraft(
                Id: id,
                Date: Date,
                Name: Name,
                PlannedAt: PlannedAtTime is null ? null : TimeOnly.FromTimeSpan(PlannedAtTime.Value),
                Duration: DurationMinutes is null ? null : TimeSpan.FromMinutes((double)DurationMinutes.Value),
                PresetCode: SelectedPreset?.Code,
                MetValue: SelectedPreset?.MetValue,
                ManualBurnedCaloriesKcal: SelectedPreset is null ? ManualBurnedCaloriesKcal : null,
                Note: Note
            )
        );

        if(result.IsSuccess) {
            onSaved();
            return;
        }

        foreach(var error in result.Errors) {
            ValidationMessages.Add(FormatError(error));
        }

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    [RelayCommand]
    private void Cancel() {
        onCancelled();
    }

    private static string FormatError(PlannedActivityValidationError error) {
        return error switch {
            PlannedActivityValidationError.MissingId => "Не удалось определить план.",
            PlannedActivityValidationError.MissingName => "Введите название активности.",
            PlannedActivityValidationError.InvalidDuration => "Длительность должна быть больше нуля.",
            PlannedActivityValidationError.InvalidMetValue => "Некорректный MET выбранного типа.",
            PlannedActivityValidationError.InvalidManualBurnedCalories => "Расход калорий должен быть больше нуля.",
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
        };
    }
}
