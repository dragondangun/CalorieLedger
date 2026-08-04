using CalorieLedger.Domain.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalorieLedger.ViewModels.Profile;

public partial class BodyMeasurementListItemViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly Action<Guid> onEdit;
    private readonly Action<Guid> onDelete;

    public Guid Id { get; }

    public string DateSummary { get; }

    public string WeightSummary { get; }

    public string AdditionalValuesSummary { get; }

    public bool HasAdditionalValues => !string.IsNullOrWhiteSpace(AdditionalValuesSummary);

    public string ChangesSummary { get; }

    public bool HasChangesSummary => !string.IsNullOrWhiteSpace(ChangesSummary);

    public bool IsLatest { get; }

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public BodyMeasurementListItemViewModel(
        BodyMeasurementEntry entry,
        Action<Guid> onEdit,
        Action<Guid> onDelete,
        BodyMeasurementEntry? previousMeasurement = null,
        bool isLatest = false)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(onEdit);
        ArgumentNullException.ThrowIfNull(onDelete);

        Id = entry.Id;
        DateSummary = FormatDate(entry.Date);
        WeightSummary = FormatWeight(entry.WeightKg);
        AdditionalValuesSummary = FormatAdditionalValues(entry);
        ChangesSummary = FormatChanges(
            entry,
            previousMeasurement
        );

        IsLatest = isLatest;

        this.onEdit = onEdit;
        this.onDelete = onDelete;
    }

    [RelayCommand]
    private void Edit() {
        onEdit(Id);
    }

    [RelayCommand]
    private void Delete() {
        IsDeleteConfirmationVisible = true;
    }

    [RelayCommand]
    private void ConfirmDelete() {
        IsDeleteConfirmationVisible = false;
        onDelete(Id);
    }

    [RelayCommand]
    private void CancelDelete() {
        IsDeleteConfirmationVisible = false;
    }

    partial void OnIsDeleteConfirmationVisibleChanged(bool value) {
        OnPropertyChanged(nameof(ArePrimaryActionsVisible));
    }

    private static string BuildAdditionalValuesSummary(BodyMeasurementEntry entry) {
        var values = new List<string>();

        if(entry.BodyFatPercent is not null) {
            values.Add(
                $"жир {entry.BodyFatPercent.Value.ToString(
                    "0.0",
                    RussianCulture)}%");
        }

        if(entry.BoneMassKg is not null) {
            values.Add(
                $"кости {entry.BoneMassKg.Value.ToString(
                    "0.0",
                    RussianCulture)} кг");
        }

        if(entry.MuscleMassKg is not null) {
            values.Add(
                $"мышцы {entry.MuscleMassKg.Value.ToString(
                    "0.0",
                    RussianCulture)} кг");
        }

        if(entry.MusclePercent is not null) {
            values.Add(
                $"мышцы {entry.MusclePercent.Value.ToString(
                    "0.0",
                    RussianCulture)}%");
        }

        return string.Join(
            " · ",
            values);
    }

    private static string FormatChanges(BodyMeasurementEntry entry, BodyMeasurementEntry? previousMeasurement) {
        if(previousMeasurement is null) {
            return string.Empty;
        }

        var values = new List<string>();

        AddDifference(
            values,
            label: "вес",
            difference: entry.WeightKg - previousMeasurement.WeightKg,
            suffix: " кг"
        );

        if(entry.BodyFatPercent is decimal bodyFatPercent
            && previousMeasurement.BodyFatPercent is decimal previousBodyFatPercent) {
            AddDifference(
                values,
                label: "жир",
                difference: bodyFatPercent - previousBodyFatPercent,
                suffix: " п.п."
            );
        }

        if(entry.MuscleMassKg is decimal muscleMassKg
            && previousMeasurement.MuscleMassKg is decimal previousMuscleMassKg) {
            AddDifference(
                values,
                label: "мышцы",
                difference: muscleMassKg - previousMuscleMassKg,
                suffix: " кг"
            );
        }
        else if(entry.MusclePercent is decimal musclePercent
            && previousMeasurement.MusclePercent is decimal previousMusclePercent) {
            AddDifference(
                values,
                label: "мышцы",
                difference: musclePercent - previousMusclePercent,
                suffix: " п.п."
            );
        }

        if(entry.BoneMassKg is decimal boneMassKg
            && previousMeasurement.BoneMassKg is decimal previousBoneMassKg) {
            AddDifference(
                values,
                label: "кости",
                difference: boneMassKg - previousBoneMassKg,
                suffix: " кг"
            );
        }

        var dayCount = Math.Abs(
            entry.Date.DayNumber
            - previousMeasurement.Date.DayNumber
        );

        var periodSummary = $"За {FormatDayCount(dayCount)}";

        return values.Count == 0
            ? $"{periodSummary}: без изменений"
            : $"{periodSummary}: {string.Join(" · ", values)}";
    }

    private static void AddDifference(
        ICollection<string> values,
        string label,
        decimal difference,
        string suffix)
    {
        if(difference == 0m) {
            return;
        }

        values.Add(
            $"{label} {FormatSignedDifference(difference, suffix)}"
        );
    }

    private static string FormatDayCount(int dayCount) {
        var lastTwoDigits = dayCount % 100;

        var suffix = lastTwoDigits is >= 11 and <= 14
            ? "дней"
            : (dayCount % 10) switch {
                1 => "день",
                2 or 3 or 4 => "дня",
                _ => "дней",
            };

        return $"{dayCount} {suffix}";
    }

    private static string FormatSignedDifference(
        decimal difference,
        string suffix)
    {
        var sign = difference switch {
            > 0m => "+",
            < 0m => "−",
            _ => string.Empty,
        };

        return $"{sign}{Math.Abs(difference).ToString("0.0", RussianCulture)}{suffix}";
    }

    private static string FormatDate(DateOnly date) {
        return date.ToString(
            "dd.MM.yyyy",
            RussianCulture
        );
    }

    private static string FormatWeight(decimal weightKg) {
        return $"{weightKg.ToString("0.0", RussianCulture)} кг";
    }

    private static string FormatAdditionalValues(BodyMeasurementEntry entry) {
        var values = new List<string>();

        if(entry.BodyFatPercent is decimal bodyFatPercent) {
            values.Add(
                $"жир {bodyFatPercent.ToString("0.0", RussianCulture)}%"
            );
        }

        if(entry.BoneMassKg is decimal boneMassKg) {
            values.Add(
                $"кости {boneMassKg.ToString("0.0", RussianCulture)} кг"
            );
        }

        if(entry.MuscleMassKg is decimal muscleMassKg) {
            values.Add(
                $"мышцы {muscleMassKg.ToString("0.0", RussianCulture)} кг"
            );
        }

        if(entry.MusclePercent is decimal musclePercent) {
            values.Add(
                $"мышцы {musclePercent.ToString("0.0", RussianCulture)}%"
            );
        }

        return string.Join(
            " · ",
            values
        );
    }
}