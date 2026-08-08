using CalorieLedger.Domain.Profile;
using System;
using System.Collections.Generic;
using System.Globalization;
using CalorieLedger.ViewModels.Common;

namespace CalorieLedger.ViewModels.Profile;

public static class BodyMeasurementListItemPresentationFactory {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static BodyMeasurementListItemPresentation Create(
        BodyMeasurementEntry entry,
        BodyMeasurementEntry? previousMeasurement = null,
        bool isLatest = false,
        DateOnly? currentDate = null
    ) {
        ArgumentNullException.ThrowIfNull(entry);
        var freshnessState = GetFreshnessState(
            entry.Date,
            currentDate,
            isLatest
        );

        var measurementAgeDays = GetMeasurementAgeInDays(
            entry.Date,
            currentDate,
            isLatest
        );

        return new BodyMeasurementListItemPresentation(
            DateSummary: FormatDate(entry.Date),
            WeightSummary: FormatWeight(entry.WeightKg),
            AdditionalValuesSummary: FormatAdditionalValues(entry),
            ChangesSummary: FormatChanges(
                entry,
                previousMeasurement
            ),
            DataCompletenessText: FormatDataCompleteness(entry),
            IsLatest: isLatest,
            LatestBadgeText: FormatLatestBadgeText(
                freshnessState,
                measurementAgeDays
            ),
            IsLatestMeasurementStale: freshnessState == BodyMeasurementFreshnessState.Stale,
            MeasurementFreshnessWarning: FormatMeasurementFreshnessWarning(freshnessState)
        );
    }

    private static string FormatDate(DateOnly date) {
        return date.ToString("dd.MM.yyyy", RussianCulture);
    }

    private static string FormatWeight(decimal weightKg) {
        return $"{weightKg.ToString("0.0", RussianCulture)} кг";
    }

    private static string FormatAdditionalValues(BodyMeasurementEntry entry) {
        var values = new List<string>();

        if(entry.BodyFatPercent is decimal bodyFatPercent) {
            values.Add($"жир {bodyFatPercent.ToString("0.0", RussianCulture)}%");
        }

        if(entry.BoneMassKg is decimal boneMassKg) {
            values.Add($"кости {boneMassKg.ToString("0.0", RussianCulture)} кг");
        }

        if(entry.MuscleMassKg is decimal muscleMassKg) {
            values.Add($"мышцы {muscleMassKg.ToString("0.0", RussianCulture)} кг");
        }

        if(entry.MusclePercent is decimal musclePercent) {
            values.Add($"доля мышц {musclePercent.ToString("0.0", RussianCulture)}%");
        }

        return string.Join(
            " · ",
            values
        );
    }

    private static string FormatDataCompleteness(BodyMeasurementEntry entry) {
        var missingValues = new List<string>();

        if(entry.BodyFatPercent is null) {
            missingValues.Add("жир");
        }

        if(entry.BoneMassKg is null) {
            missingValues.Add("кости");
        }

        if(entry.MuscleMassKg is null
            && entry.MusclePercent is null) {
            missingValues.Add("мышцы");
        }

        return missingValues.Count switch {
            0 => string.Empty,
            3 => "Указан только вес",
            1 => $"Не указано: {missingValues[0]}",
            _ => $"Не указаны: {string.Join(", ", missingValues)}",
        };
    }

    private static string FormatChanges(
        BodyMeasurementEntry entry,
        BodyMeasurementEntry? previousMeasurement
    ) {
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

        var dayCount = Math.Abs(entry.Date.DayNumber - previousMeasurement.Date.DayNumber);

        var periodSummary = $"За {RussianDayCountFormatter.Format(dayCount)}";

        return values.Count == 0
            ? $"{periodSummary}: без изменений"
            : $"{periodSummary}: {string.Join(" · ", values)}";
    }

    private static void AddDifference(
        ICollection<string> values,
        string label,
        decimal difference,
        string suffix
    ) {
        var roundedDifference = decimal.Round(
            difference,
            decimals: 1,
            mode: MidpointRounding.AwayFromZero
        );

        if(roundedDifference == 0m) {
            return;
        }

        values.Add($"{label} {FormatSignedDifference(roundedDifference, suffix)}");
    }

    private static string FormatSignedDifference(
        decimal difference,
        string suffix
    ) {
        var sign = difference switch {
            > 0m => "+",
            < 0m => "−",
            _ => string.Empty,
        };

        return $"{sign}{Math.Abs(difference).ToString("0.0", RussianCulture)}{suffix}";
    }

    private static string FormatLatestBadgeText(
        BodyMeasurementFreshnessState freshnessState,
        int? measurementAgeDays
    ) {
        return freshnessState switch {
            BodyMeasurementFreshnessState.NotApplicable => string.Empty,
            BodyMeasurementFreshnessState.Future => "Последнее · будущая дата",
            _ when measurementAgeDays == 0 => "Последнее · сегодня",
            _ when measurementAgeDays == 1 => "Последнее · вчера",
            _ => $"Последнее · {RussianDayCountFormatter.Format(measurementAgeDays!.Value)} назад",
        };
    }

    private static string FormatMeasurementFreshnessWarning(BodyMeasurementFreshnessState freshnessState) {
        if(freshnessState != BodyMeasurementFreshnessState.Stale) {
            return string.Empty;
        }

        return $"Последнее измерение сделано более {RussianDayCountFormatter.Format(BodyMeasurementFreshnessPolicy.WarningDayCount)} назад.";
    }

    private static BodyMeasurementFreshnessState GetFreshnessState(
        DateOnly measurementDate,
        DateOnly? currentDate,
        bool isLatest
    ) {
        if(!isLatest || currentDate is null) {
            return BodyMeasurementFreshnessState.NotApplicable;
        }

        return BodyMeasurementFreshnessPolicy.GetState(
            measurementDate,
            currentDate.Value
        );
    }

    private static int? GetMeasurementAgeInDays(
        DateOnly measurementDate,
        DateOnly? currentDate,
        bool isLatest
    ) {
        if(!isLatest || currentDate is null) {
            return null;
        }

        return BodyMeasurementFreshnessPolicy.GetAgeInDays(
            measurementDate,
            currentDate.Value
        );
    }
}
