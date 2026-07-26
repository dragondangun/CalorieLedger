using System;
using System.Collections.Generic;
using System.Globalization;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.ViewModels.Profile;

public static class UserNutritionProfileSummaryViewModelFactory {
    private const int FreshMeasurementMaximumAgeDays = 14;

    private static readonly CultureInfo russianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static UserNutritionProfileSummaryViewModel Create(
        UserNutritionProfile profile,
        BodyMeasurementEntry? latestMeasurement,
        DateOnly currentDate,
        Action editProfile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(editProfile);

        var personalDataSummary = $"{FormatSex(profile.Body.Sex)} · {FormatAge(profile.Body.AgeYears)} · {profile.Body.HeightCm.ToString("0.0", russianCulture)} см";

        var weightSourceSummary = FormatWeightSource(latestMeasurement);

        var measurementWarning = FormatMeasurementWarning(
            latestMeasurement,
            currentDate
        );

        return new UserNutritionProfileSummaryViewModel(
            displayName: profile.DisplayName,
            personalDataSummary: personalDataSummary,
            activitySummary: $"Активность: {FormatActivity(profile.LifestyleActivityLevel)}",
            weightSummary: $"Вес: {profile.Body.WeightKg.ToString("0.0", russianCulture)} кг",
            weightSourceSummary: weightSourceSummary,
            bodyCompositionSummary: FormatBodyComposition(profile.Body),
            measurementWarning: measurementWarning,
            editProfile: editProfile
        );
    }

    private static string FormatWeightSource(BodyMeasurementEntry? latestMeasurement) {
        if(latestMeasurement is null) {
            return "Источник веса: исходные данные профиля";
        }

        var measurementDate = latestMeasurement.Date.ToString(
            "dd.MM.yyyy",
            russianCulture
        );

        return $"Последнее измерение: {measurementDate}";
    }

    private static string FormatMeasurementWarning(
        BodyMeasurementEntry? latestMeasurement,
        DateOnly currentDate)
    {
        if(latestMeasurement is null) {
            return "Добавьте измерение тела, чтобы вес и состав тела обновлялись по истории измерений.";
        }

        var measurementAgeDays = Math.Max(0, currentDate.DayNumber - latestMeasurement.Date.DayNumber);

        if(measurementAgeDays <= FreshMeasurementMaximumAgeDays) {
            return string.Empty;
        }

        return $"Последнему измерению {FormatDays(measurementAgeDays)}. Добавьте новое измерение, чтобы расчёты использовали свежие данные.";
    }

    private static string FormatBodyComposition(BodyProfile body) {
        var values = new List<string>();

        if(body.BodyFatPercent is decimal bodyFatPercent) {
            values.Add(
                $"жир {bodyFatPercent.ToString("0.0", russianCulture)}%"
            );
        }

        if(body.MuscleMassKg is decimal muscleMassKg) {
            values.Add(
                $"мышцы {muscleMassKg.ToString("0.0", russianCulture)} кг"
            );
        }

        if(body.MusclePercent is decimal musclePercent) {
            values.Add(
                $"{musclePercent.ToString("0.0", russianCulture)}%"
            );
        }

        if(body.BoneMassKg is decimal boneMassKg) {
            values.Add(
                $"кости {boneMassKg.ToString("0.0", russianCulture)} кг"
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

    private static string FormatDays(int dayCount) {
        return $"{dayCount} {GetCountSuffix(dayCount, "день", "дня", "дней")}";
    }

    private static string GetCountSuffix(
        int value,
        string singular,
        string paucal,
        string plural)
    {
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