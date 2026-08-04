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
    private readonly Action? onAddMeasurement;

    public Guid Id { get; }

    public string DateSummary { get; }

    public string WeightSummary { get; }

    public string AdditionalValuesSummary { get; }

    public bool HasAdditionalValues => !string.IsNullOrWhiteSpace(AdditionalValuesSummary);

    public string ChangesSummary { get; }

    public bool HasChangesSummary => !string.IsNullOrWhiteSpace(ChangesSummary);

    public bool IsLatest { get; }

    private const int MeasurementFreshnessWarningDayCount = 14;

    public string LatestBadgeText { get; }

    public bool IsLatestMeasurementStale { get; }

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public bool CanAddMeasurement => IsLatestMeasurementStale && onAddMeasurement is not null;

    public BodyMeasurementListItemViewModel(
        BodyMeasurementEntry entry,
        Action<Guid> onEdit,
        Action<Guid> onDelete,
        BodyMeasurementEntry? previousMeasurement = null,
        bool isLatest = false,
        DateOnly? currentDate = null,
        Action? onAddMeasurement = null)
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

        LatestBadgeText = FormatLatestBadgeText(
            measurementDate: entry.Date,
            currentDate: currentDate,
            isLatest: isLatest
        );

        IsLatestMeasurementStale = IsMeasurementStale(
            measurementDate: entry.Date,
            currentDate: currentDate,
            isLatest: isLatest
        );

        this.onEdit = onEdit;
        this.onDelete = onDelete;
        this.onAddMeasurement = onAddMeasurement;
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

    [RelayCommand(CanExecute = nameof(CanAddMeasurement))]
    private void AddMeasurement() {
        onAddMeasurement?.Invoke();
    }

    partial void OnIsDeleteConfirmationVisibleChanged(bool value) {
        OnPropertyChanged(nameof(ArePrimaryActionsVisible));
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
                label: "доля мышц",
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
                $"доля мышц {musclePercent.ToString("0.0", RussianCulture)}%"
            );
        }

        return string.Join(
            " · ",
            values
        );
    }

    private static string FormatLatestBadgeText(
        DateOnly measurementDate,
        DateOnly? currentDate,
        bool isLatest)
    {
        if(!isLatest) {
            return string.Empty;
        }

        if(currentDate is null) {
            return "Последнее";
        }

        var dayCount = currentDate.Value.DayNumber - measurementDate.DayNumber;

        return dayCount switch {
            < 0 => "Последнее · будущая дата",
            0 => "Последнее · сегодня",
            1 => "Последнее · вчера",
            _ => $"Последнее · {FormatDayCount(dayCount)} назад",
        };
    }

    private static bool IsMeasurementStale(
        DateOnly measurementDate,
        DateOnly? currentDate,
        bool isLatest)
    {
        if(!isLatest || currentDate is null) {
            return false;
        }

        var dayCount = currentDate.Value.DayNumber - measurementDate.DayNumber;

        return dayCount > MeasurementFreshnessWarningDayCount;
    }
}