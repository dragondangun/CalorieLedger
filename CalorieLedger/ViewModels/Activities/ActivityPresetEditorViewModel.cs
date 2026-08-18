using CalorieLedger.Application.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class ActivityPresetEditorViewModel:ViewModelBase {
    private readonly ActivityPresetCatalogService presetService;
    private readonly string code;
    private readonly Action onSaved;
    private readonly Action onCancelled;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private decimal? metValue;

    public string Title { get; }
    public ObservableCollection<string> ValidationMessages { get; } = [];
    public bool HasValidationErrors => ValidationMessages.Count > 0;

    public ActivityPresetEditorViewModel(
        ActivityPresetCatalogService presetService,
        ActivityPresetDraft draft,
        bool isNew,
        Action onSaved,
        Action onCancelled
    ) {
        this.presetService = presetService;
        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        code = draft.Code;
        Name = draft.Name;
        MetValue = draft.MetValue;
        Title = isNew ? "Новый тип активности" : "Редактирование типа активности";
    }

    [RelayCommand]
    private void Save() {
        ValidationMessages.Clear();

        var result = presetService.Save(new ActivityPresetDraft(code, Name, MetValue));

        if(result.IsSuccess) {
            onSaved();
            return;
        }

        foreach(var error in result.Errors) {
            ValidationMessages.Add(FormatValidationError(error));
        }

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    [RelayCommand]
    private void Cancel() {
        onCancelled();
    }

    private static string FormatValidationError(ActivityPresetValidationError error) {
        return error switch {
            ActivityPresetValidationError.MissingCode => "Не удалось определить тип активности.",
            ActivityPresetValidationError.MissingName => "Введите название.",
            ActivityPresetValidationError.InvalidMetValue => "MET должен быть не меньше 1.",
            ActivityPresetValidationError.DuplicateName => "Тип активности с таким названием уже существует.",
            ActivityPresetValidationError.BuiltInPresetCannotBeChanged => "Встроенный тип активности нельзя изменить.",
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
        };
    }
}
