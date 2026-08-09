using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalorieLedger.ViewModels.Profile;

public static class UserNutritionProfileSummaryViewModelFactory {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static UserNutritionProfileSummaryViewModel Create(
        UserNutritionProfile profile,
        BodyMeasurementHistorySnapshot measurementSnapshot,
        Action editProfile,
        Action addBodyMeasurement
    ) {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(editProfile);
        ArgumentNullException.ThrowIfNull(addBodyMeasurement);
        ArgumentNullException.ThrowIfNull(measurementSnapshot);

        var personalDataSummary = $"{FormatSex(profile.Body.Sex)} · {FormatAge(profile.Body.AgeYears)} · {profile.Body.HeightCm.ToString("0.0", RussianCulture)} см";

        var weightSourceSummary = FormatWeightSource(
            measurementSnapshot.LatestEffectiveMeasurement
        );

        var measurementWarning = FormatMeasurementWarning(
            measurementSnapshot.LatestEffectiveMeasurement,
            measurementSnapshot.HasFutureMeasurements,
            measurementSnapshot.AsOfDate
        );

        return new UserNutritionProfileSummaryViewModel(
            displayName: profile.DisplayName,
            personalDataSummary: personalDataSummary,
            activitySummary: $"Активность: {FormatActivity(profile.LifestyleActivityLevel)}",
            weightSummary: $"Вес: {profile.Body.WeightKg.ToString("0.0", RussianCulture)} кг",
            weightSourceSummary: weightSourceSummary,
            bodyCompositionSummary: FormatBodyComposition(profile.Body),
            measurementWarning: measurementWarning,
            editProfile: editProfile,
            addBodyMeasurement: addBodyMeasurement
        );
    }

    private static string FormatWeightSource(BodyMeasurementEntry? effectiveMeasurement) {
        if(effectiveMeasurement is null) {
            return "Источник веса: исходные данные профиля";
        }

        var measurementDate = effectiveMeasurement.Date.ToString(
            "dd.MM.yyyy",
            RussianCulture
        );

        return $"Источник веса: измерение от {measurementDate}";
    }

    private static string FormatMeasurementWarning(
        BodyMeasurementEntry? effectiveMeasurement,
        bool hasFutureMeasurements,
        DateOnly currentDate
    ) {
        if(hasFutureMeasurements) {
            return "В истории есть измерение с будущей датой. Проверьте дату измерения.";
        }

        if(effectiveMeasurement is null) {
            return "Добавьте измерение тела, чтобы вес и состав тела обновлялись по истории измерений.";
        }

        var measurementAgeDays = BodyMeasurementFreshnessPolicy.GetAgeInDays(
            effectiveMeasurement.Date,
            currentDate
        );

        var freshnessState = BodyMeasurementFreshnessPolicy.GetState(measurementAgeDays);

        if(freshnessState != BodyMeasurementFreshnessState.Stale) {
            return string.Empty;
        }

        return $"Последнему измерению {RussianDayCountFormatter.Format(measurementAgeDays)}. Добавьте новое измерение, чтобы расчёты использовали свежие данные.";
    }

    private static string FormatBodyComposition(BodyProfile body) {
        var values = new List<string>();

        if(body.BodyFatPercent is decimal bodyFatPercent) {
            values.Add(
                $"жир {bodyFatPercent.ToString("0.0", RussianCulture)}%"
            );
        }

        if(body.MuscleMassKg is decimal muscleMassKg) {
            values.Add(
                $"мышцы {muscleMassKg.ToString("0.0", RussianCulture)} кг"
            );
        }

        if(body.MusclePercent is decimal musclePercent) {
            values.Add(
                $"{musclePercent.ToString("0.0", RussianCulture)}%"
            );
        }

        if(body.BoneMassKg is decimal boneMassKg) {
            values.Add(
                $"кости {boneMassKg.ToString("0.0", RussianCulture)} кг"
            );
        }

        return string.Join(" · ", values);
    }

    private static string FormatSex(BiologicalSex sex) {
        return sex switch {
            BiologicalSex.Female => "Женский пол",
            BiologicalSex.Male => "Мужской пол",
            BiologicalSex.Unknown => "Пол не указан",
            _ => "Пол не указан",
        };
    }

    private static string FormatAge(int ageYears) {
        return $"{ageYears} {GetCountSuffix(ageYears, "год", "года", "лет")}";
    }

    private static string GetCountSuffix(
        int value,
        string singular,
        string paucal,
        string plural
    ) {
        var lastTwoDigits = value % 100;

        if(lastTwoDigits is >= 11 and <= 14) {
            return plural;
        }

        return (value % 10) switch {
            1 => singular,
            2 or 3 or 4 => paucal,
            _ => plural,
        };
    }

    private static string FormatActivity(LifestyleActivityLevel activityLevel) {
        return activityLevel switch {
            LifestyleActivityLevel.Sedentary => "минимальная",

            LifestyleActivityLevel.LightlyActive => "лёгкая",

            LifestyleActivityLevel.ModeratelyActive => "умеренная",

            LifestyleActivityLevel.VeryActive => "высокая",

            LifestyleActivityLevel.ExtremelyActive => "очень высокая",

            _ => "не указана",
        };
    }
}
