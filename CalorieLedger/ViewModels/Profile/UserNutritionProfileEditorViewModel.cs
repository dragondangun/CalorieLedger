using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalorieLedger.ViewModels.Profile;

public partial class UserNutritionProfileEditorViewModel:ViewModelBase {
    private readonly UserNutritionProfileEditorService editorService;
    private readonly Guid profileId;
    private readonly Action onSaved;
    private readonly Action onCancelled;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private BiologicalSexOptionViewModel selectedSexOption = null!;

    [ObservableProperty]
    private int? ageYears;

    [ObservableProperty]
    private decimal? heightCm;

    [ObservableProperty]
    private LifestyleActivityLevelOptionViewModel selectedActivityLevelOption = null!;

    [ObservableProperty]
    private string statusSummary = string.Empty;

    public IReadOnlyList<BiologicalSexOptionViewModel> SexOptions { get; } = [
        new(
            Value: BiologicalSex.Unknown,
            Title: "Не указан"
        ),
        new(
            Value: BiologicalSex.Female,
            Title: "Женский"
        ),
        new(
            Value: BiologicalSex.Male,
            Title: "Мужской"
        ),
    ];

    public IReadOnlyList<LifestyleActivityLevelOptionViewModel> ActivityLevelOptions { get; } = [
        new(
            Value: LifestyleActivityLevel.Sedentary,
            Title: "Минимальная активность",
            Description: "Преимущественно сидячий образ жизни."
        ),
        new(
            Value: LifestyleActivityLevel.LightlyActive,
            Title: "Лёгкая активность",
            Description: "Небольшая нагрузка примерно 1–3 раза в неделю."
        ),
        new(
            Value: LifestyleActivityLevel.ModeratelyActive,
            Title: "Умеренная активность",
            Description: "Регулярная нагрузка примерно 3–5 раз в неделю."
        ),
        new(
            Value: LifestyleActivityLevel.VeryActive,
            Title: "Высокая активность",
            Description: "Интенсивная нагрузка примерно 6–7 раз в неделю."
        ),
        new(
            Value: LifestyleActivityLevel.ExtremelyActive,
            Title: "Очень высокая активность",
            Description: "Тяжёлая физическая работа или очень большие тренировочные нагрузки."
        ),
    ];

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public bool HasValidationErrors => ValidationMessages.Count > 0;

    public UserNutritionProfileEditorViewModel(
        UserNutritionProfileEditorService editorService,
        UserNutritionProfileDraft draft,
        Action onSaved,
        Action onCancelled
    ) {
        ArgumentNullException.ThrowIfNull(editorService);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(onSaved);
        ArgumentNullException.ThrowIfNull(onCancelled);

        this.editorService = editorService;
        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        profileId = draft.Id;
        DisplayName = draft.DisplayName;
        AgeYears = draft.AgeYears;
        HeightCm = draft.HeightCm;

        SelectedSexOption = SexOptions.Single(
            option => option.Value == draft.Sex
        );

        SelectedActivityLevelOption = ActivityLevelOptions.Single(
            option => option.Value == draft.LifestyleActivityLevel
        );
    }

    [RelayCommand]
    private void Save() {
        ClearValidationMessages();

        var draft = new UserNutritionProfileDraft(
            Id: profileId,
            DisplayName: DisplayName,
            Sex: SelectedSexOption.Value,
            AgeYears: AgeYears,
            HeightCm: HeightCm,
            LifestyleActivityLevel: SelectedActivityLevelOption.Value
        );

        var result = editorService.Save(draft);

        if(!result.IsSuccess) {
            foreach(var error in result.Errors) {
                ValidationMessages.Add(
                    FormatValidationError(error)
                );
            }

            StatusSummary = "Профиль не сохранён. Исправьте указанные ошибки.";
            OnPropertyChanged(nameof(HasValidationErrors));

            return;
        }

        StatusSummary = "Профиль успешно сохранён.";
        onSaved();
    }

    [RelayCommand]
    private void Cancel() {
        StatusSummary = "Изменения отменены.";
        onCancelled();
    }

    private void ClearValidationMessages() {
        ValidationMessages.Clear();
        StatusSummary = string.Empty;

        OnPropertyChanged(nameof(HasValidationErrors));
    }

    private static string FormatValidationError(UserNutritionProfileValidationError error) {
        return error switch {
            UserNutritionProfileValidationError.MissingId => "Не удалось определить редактируемый профиль.",

            UserNutritionProfileValidationError.ProfileIdMismatch => "Профиль был изменён. Закройте форму и откройте её заново.",

            UserNutritionProfileValidationError.MissingDisplayName => "Введите имя пользователя.",

            UserNutritionProfileValidationError.InvalidSex => "Выбрано некорректное значение пола.",

            UserNutritionProfileValidationError.InvalidAge => "Возраст должен находиться в диапазоне от 1 до 120 лет.",

            UserNutritionProfileValidationError.InvalidHeight => "Рост должен находиться в диапазоне от 50 до 250 см.",

            UserNutritionProfileValidationError.InvalidLifestyleActivityLevel => "Выбран некорректный уровень активности.",

            _ => $"Неизвестная ошибка проверки: {error}."
        };
    }
}
