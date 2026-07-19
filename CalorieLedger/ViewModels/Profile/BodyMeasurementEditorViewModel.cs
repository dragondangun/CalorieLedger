using System;
using System.Collections.ObjectModel;
using CalorieLedger.Application.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Globalization;

namespace CalorieLedger.ViewModels.Profile;

public partial class BodyMeasurementEditorViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly BodyMeasurementEditorService editorService;

    private readonly DateOnly currentDate;
    private readonly Guid measurementId;
    private readonly Action onSaved;
    private readonly Action onCancelled;

    private bool isSynchronizingMuscleValues;

    private MuscleValueInputSource muscleValueInputSource = MuscleValueInputSource.None;

    [ObservableProperty]
    private DateTimeOffset? measurementDate;

    [ObservableProperty]
    private decimal? weightKg;

    [ObservableProperty]
    private decimal? bodyFatPercent;

    [ObservableProperty]
    private decimal? boneMassKg;

    [ObservableProperty]
    private decimal? muscleMassKg;

    [ObservableProperty]
    private decimal? musclePercent;

    [ObservableProperty]
    private string compositionPreviewSummary = string.Empty;

    [ObservableProperty]
    private bool hasCompositionPreview;

    [ObservableProperty]
    private bool hasCompositionWarning;

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public bool HasValidationErrors => ValidationMessages.Count > 0;

    private enum MuscleValueInputSource {
        None,
        MuscleMass,
        MusclePercent
    }

    public BodyMeasurementEditorViewModel(
        BodyMeasurementEditorService editorService,
        BodyMeasurementDraft draft,
        DateOnly currentDate,
        Action onSaved,
        Action onCancelled) {
        ArgumentNullException.ThrowIfNull(
            editorService);

        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(onSaved);
        ArgumentNullException.ThrowIfNull(onCancelled);

        this.editorService = editorService;
        this.currentDate = currentDate;
        this.onSaved = onSaved;
        this.onCancelled = onCancelled;

        measurementId = draft.Id;

        isSynchronizingMuscleValues = true;
        
        MeasurementDate = new DateTimeOffset(
            draft.Date.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        WeightKg = draft.WeightKg;
        BodyFatPercent = draft.BodyFatPercent;
        BoneMassKg = draft.BoneMassKg;
        MuscleMassKg = draft.MuscleMassKg;
        MusclePercent = draft.MusclePercent;

        isSynchronizingMuscleValues = false;

        muscleValueInputSource = draft.MuscleMassKg is not null
            ? MuscleValueInputSource.MuscleMass
            : draft.MusclePercent is not null
                ? MuscleValueInputSource.MusclePercent
                    : MuscleValueInputSource.None;

        UpdateCompositionPreview();
    }

    partial void OnWeightKgChanged(decimal? value) {
        if(isSynchronizingMuscleValues) {
            return;
        }

        switch(muscleValueInputSource) {
            case MuscleValueInputSource.MuscleMass:
                RecalculateMusclePercent();
                break;

            case MuscleValueInputSource.MusclePercent:
                RecalculateMuscleMass();
                break;
        }

        UpdateCompositionPreview();
    }

    partial void OnBodyFatPercentChanged(decimal? value) {
        UpdateCompositionPreview();
    }

    partial void OnBoneMassKgChanged(decimal? value) {
        UpdateCompositionPreview();
    }

    partial void OnMuscleMassKgChanged(decimal? value) {
        if(isSynchronizingMuscleValues) {
            return;
        }

        muscleValueInputSource = value is null
            ? MuscleValueInputSource.None
            : MuscleValueInputSource.MuscleMass;

        RecalculateMusclePercent();
        UpdateCompositionPreview();
    }

    partial void OnMusclePercentChanged(decimal? value) {
        if(isSynchronizingMuscleValues) {
            return;
        }

        muscleValueInputSource = value is null
            ? MuscleValueInputSource.None
            : MuscleValueInputSource.MusclePercent;

        RecalculateMuscleMass();
        UpdateCompositionPreview();
    }

    private void RecalculateMusclePercent() {
        isSynchronizingMuscleValues = true;

        try {
            if(WeightKg is not > 0m
                || MuscleMassKg is not > 0m) {
                MusclePercent = null;
                return;
            }

            MusclePercent = BodyMeasurementMuscleValueNormalizer
                .CalculateMusclePercent(
                    weightKg: WeightKg.Value,
                    muscleMassKg: MuscleMassKg.Value);
        }
        finally {
            isSynchronizingMuscleValues = false;
        }
    }

    private void RecalculateMuscleMass() {
        isSynchronizingMuscleValues = true;

        try {
            if(WeightKg is not > 0m
                || MusclePercent is not > 0m
                or >= 100m) {
                MuscleMassKg = null;
                return;
            }

            MuscleMassKg = BodyMeasurementMuscleValueNormalizer.CalculateMuscleMass(
                weightKg: WeightKg.Value,
                musclePercent: MusclePercent.Value);
        }
        finally {
            isSynchronizingMuscleValues = false;
        }
    }

    [RelayCommand]
    private void Save() {
        ClearValidationMessages();

        if(MeasurementDate is null) {
            AddValidationMessage(
                "Укажите дату измерения.");

            return;
        }

        var measurementDate = DateOnly.FromDateTime(MeasurementDate.Value.DateTime);

        var draft = CreateCurrentDraft(measurementDate);

        var result = editorService.Save(
            draft,
            currentDate);

        if(result.IsSuccess) {
            onSaved();
            return;
        }

        foreach(var error in result.Errors) {
            AddValidationMessage(GetValidationMessage(error));
        }
    }

    [RelayCommand]
    private void Cancel() {
        onCancelled();
    }

    private void ClearValidationMessages() {
        ValidationMessages.Clear();

        OnPropertyChanged(
            nameof(HasValidationErrors));
    }

    private void AddValidationMessage(
        string message) {
        ValidationMessages.Add(message);

        OnPropertyChanged(
            nameof(HasValidationErrors));
    }

    private BodyMeasurementDraft CreateCurrentDraft(DateOnly date) {
        return new BodyMeasurementDraft(
            Id: measurementId,
            Date: date,
            WeightKg: WeightKg,
            BodyFatPercent: BodyFatPercent,
            BoneMassKg: BoneMassKg,
            MuscleMassKg: MuscleMassKg,
            MusclePercent: MusclePercent);
    }

    private void UpdateCompositionPreview() {
        var previewDate = MeasurementDate is null
            ? currentDate
            : DateOnly.FromDateTime(MeasurementDate.Value.DateTime);

        var draft = CreateCurrentDraft(previewDate);

        var result = editorService.CalculateCompositionPreview(draft);

        var values = new List<string>();

        if(result.FatMassKg is decimal fatMassKg) {
            values.Add($"Жировая масса: {FormatKilograms(fatMassKg)}");
        }

        if(result.FatFreeMassKg is decimal fatFreeMassKg) {
            values.Add($"Безжировая масса: {FormatKilograms(fatFreeMassKg)}");
        }

        if(result.BonePercent is decimal bonePercent) {
            values.Add($"Доля костной массы: {FormatPercent(bonePercent)}");
        }

        if(result.ResidualMassKg is decimal residualMassKg) {
            values.Add($"Остальная масса: {FormatKilograms(residualMassKg)}");
        }

        CompositionPreviewSummary = string.Join(
            Environment.NewLine,
            values);

        HasCompositionPreview = values.Count > 0;

        HasCompositionWarning =
            HasCompositionPreview
            && !result.IsConsistent;
    }

    private static string FormatKilograms(decimal value) {
        return value.ToString(
            "0.00",
            RussianCulture) + " кг";
    }

    private static string FormatPercent(decimal value) {
        return value.ToString(
            "0.00",
            RussianCulture) + "%";
    }

    private static string GetValidationMessage(BodyMeasurementValidationError error) {
        return error switch
        {
            BodyMeasurementValidationError.MissingId =>
                "Не удалось определить запись измерения.",

            BodyMeasurementValidationError.FutureDate =>
                "Дата измерения не может быть в будущем.",

            BodyMeasurementValidationError.InvalidWeight =>
                "Укажите положительное значение веса.",

            BodyMeasurementValidationError .InvalidBodyFatPercent =>
                "Процент жира должен быть больше 0 и меньше 100.",

            BodyMeasurementValidationError.InvalidBoneMass =>
                "Костная масса должна быть положительной и не превышать вес тела.",

            BodyMeasurementValidationError.InvalidMuscleMass =>
                "Мышечная масса должна быть положительной и не превышать вес тела.",

            BodyMeasurementValidationError.InvalidMusclePercent =>
                "Процент мышц должен быть больше 0 и меньше 100.",

            BodyMeasurementValidationError.InconsistentMuscleValues =>
                "Мышечная масса и процент мышц не согласованы с весом тела.",

            BodyMeasurementValidationError.InconsistentBodyComposition =>
                "Жировая, мышечная и костная масса не согласованы с общим весом тела.",

            _ => "Не удалось сохранить измерение."
        };
    }
}