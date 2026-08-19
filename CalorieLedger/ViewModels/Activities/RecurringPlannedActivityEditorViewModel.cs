using CalorieLedger.Application.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class RecurringPlannedActivityEditorViewModel:ViewModelBase {
    private readonly RecurringPlannedActivityService service;
    private readonly Guid id;
    private readonly Action onSaved;
    private readonly Action onCancelled;

    [ObservableProperty]
    private DateOnly startDate;

    [ObservableProperty]
    private RecurringActivityWeekdayOption selectedWeekday;

    [ObservableProperty]
    private int intervalWeeks;

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

    public IReadOnlyList<RecurringActivityWeekdayOption> Weekdays => RecurringActivityWeekdayOption.All;

    public IReadOnlyList<ActivityPreset> ActivityPresets { get; }

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public bool HasValidationErrors => ValidationMessages.Count > 0;
    public bool IsManualEnergyVisible => SelectedPreset is null;
    public string Title { get; }

    public DateTimeOffset? StartDatePickerDate {
        get => new DateTimeOffset(
            StartDate.Year,
            StartDate.Month,
            StartDate.Day,
            0,
            0,
            0,
            TimeSpan.Zero
        );
        set {
            if(value is null) {
                return;
            }

            StartDate = DateOnly.FromDateTime(value.Value.DateTime);
        }
    }

    public RecurringPlannedActivityEditorViewModel(
        RecurringPlannedActivityService service,
        ActivityPresetCatalogService presetCatalogService,
        RecurringPlannedActivityDraft draft,
        bool isNew,
        Action onSaved,
        Action onCancelled
    ) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(presetCatalogService);
        ArgumentNullException.ThrowIfNull(draft);

        this.service = service;
        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        id = draft.Id;
        StartDate = draft.StartDate;
        SelectedWeekday = Weekdays.First(option => option.Value == draft.DayOfWeek);
        IntervalWeeks = draft.IntervalWeeks;
        Name = draft.Name;
        PlannedAtTime = draft.PlannedAt?.ToTimeSpan();
        DurationMinutes = draft.Duration is null
            ? null : (decimal)draft.Duration.Value.TotalMinutes;
        ManualBurnedCaloriesKcal = draft.ManualBurnedCaloriesKcal;
        Note = draft.Note;
        Title = isNew ? "Новое расписание активности" : "Редактирование расписания";

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

    partial void OnStartDateChanged(DateOnly value) {
        OnPropertyChanged(nameof(StartDatePickerDate));
    }

    [RelayCommand]
    private void UseManualEnergy() {
        SelectedPreset = null;
    }

    [RelayCommand]
    private void Save() {
        ValidationMessages.Clear();

        var result = service.Save(
            new RecurringPlannedActivityDraft(
                Id: id,
                StartDate: StartDate,
                DayOfWeek: SelectedWeekday.Value,
                IntervalWeeks: IntervalWeeks,
                Name: Name,
                PlannedAt: PlannedAtTime is null ? null : TimeOnly.FromTimeSpan(PlannedAtTime.Value),
                Duration: DurationMinutes is null
                    ? null : TimeSpan.FromMinutes((double)DurationMinutes.Value),
                PresetCode: SelectedPreset?.Code,
                MetValue: SelectedPreset?.MetValue,
                ManualBurnedCaloriesKcal: SelectedPreset is null
                    ? ManualBurnedCaloriesKcal : null,
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

    private static string FormatError(RecurringPlannedActivityValidationError error) {
        return error switch {
            RecurringPlannedActivityValidationError.MissingId => "Не удалось определить расписание.",
            RecurringPlannedActivityValidationError.MissingName => "Введите название активности.",
            RecurringPlannedActivityValidationError.InvalidInterval => "Интервал должен быть не меньше одной недели.",
            RecurringPlannedActivityValidationError.InvalidDuration => "Длительность должна быть больше нуля.",
            RecurringPlannedActivityValidationError.InvalidMetValue => "Некорректный MET.",
            RecurringPlannedActivityValidationError.InvalidManualBurnedCalories => "Расход должен быть больше нуля.",
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
        };
    }
}
