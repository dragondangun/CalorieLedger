using System;
using System.Collections.Generic;
using System.Globalization;
using CalorieLedger.Domain.Profile;
using CommunityToolkit.Mvvm.Input;

namespace CalorieLedger.ViewModels.Profile;

public partial class BodyMeasurementListItemViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly Action<Guid> onEdit;
    private readonly Action<Guid> onDelete;

    public Guid Id { get; }

    public string DateSummary { get; }

    public string WeightSummary { get; }

    public string AdditionalValuesSummary { get; }

    public bool HasAdditionalValues =>
        !string.IsNullOrWhiteSpace(
            AdditionalValuesSummary);

    public BodyMeasurementListItemViewModel(
        BodyMeasurementEntry entry,
        Action<Guid> onEdit,
        Action<Guid> onDelete) {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(onEdit);
        ArgumentNullException.ThrowIfNull(onDelete);

        this.onEdit = onEdit;
        this.onDelete = onDelete;

        Id = entry.Id;

        DateSummary = entry.Date.ToString("dd.MM.yyyy");

        WeightSummary = $"{entry.WeightKg.ToString("0.0", RussianCulture)} кг";

        AdditionalValuesSummary = BuildAdditionalValuesSummary(entry);
    }

    [RelayCommand]
    private void Edit() {
        onEdit(Id);
    }

    [RelayCommand]
    private void Delete() {
        onDelete(Id);
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
}