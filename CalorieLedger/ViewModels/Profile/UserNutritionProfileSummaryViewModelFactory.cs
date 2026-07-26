using CalorieLedger.Domain.Profile;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalorieLedger.ViewModels.Profile;

public static class UserNutritionProfileSummaryViewModelFactory {
    private static readonly CultureInfo russianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static UserNutritionProfileSummaryViewModel Create(
        UserNutritionProfile profile,
        Action editProfile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(editProfile);

        var personalDataSummary = $"{FormatSex(profile.Body.Sex)} · {FormatAge(profile.Body.AgeYears)} · {profile.Body.HeightCm.ToString("0.0", russianCulture)} см";

        var bodyCompositionSummary = FormatBodyComposition(profile.Body);

        return new UserNutritionProfileSummaryViewModel(
            displayName: profile.DisplayName,
            personalDataSummary: personalDataSummary,
            activitySummary: $"Активность: {FormatActivity(profile.LifestyleActivityLevel)}",
            weightSummary: $"Актуальный вес: {profile.Body.WeightKg.ToString("0.0", russianCulture)} кг",
            bodyCompositionSummary: bodyCompositionSummary,
            editProfile: editProfile
        );
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
        var lastTwoDigits = ageYears % 100;
        var lastDigit = ageYears % 10;

        var suffix = lastTwoDigits is >= 11 and <= 14
            ? "лет"
            : lastDigit switch
            {
                1 => "год",
                2 or 3 or 4 => "года",
                _ => "лет",
            };

        return $"{ageYears} {suffix}";
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