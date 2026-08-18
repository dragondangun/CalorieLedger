using CalorieLedger.Application.Activities;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace CalorieLedger.ViewModels.Activities;

public partial class ActivityEditorViewModel:ViewModelBase {
    private readonly ActivityEditorService editorService;
    private readonly Guid activityId;
    private readonly DateOnly activityDate;
    private readonly DateOnly currentDate;
    private readonly Action onSaved;
    private readonly Action onCancelled;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private decimal? burnedCaloriesKcal;

    [ObservableProperty]
    private TimeSpan? startedAtTime;

    [ObservableProperty]
    private decimal? durationMinutes;

    [ObservableProperty]
    private string? note;

    public string Title { get; }

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public bool HasValidationErrors => ValidationMessages.Count > 0;

    public ActivityEditorViewModel(
        ActivityEditorService editorService,
        ActivityDraft draft,
        DateOnly currentDate,
        bool isNew,
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
            Note: Note
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
}
