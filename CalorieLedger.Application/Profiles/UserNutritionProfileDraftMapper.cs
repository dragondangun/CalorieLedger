using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public static class UserNutritionProfileDraftMapper {
    public static UserNutritionProfileDraft FromProfile(UserNutritionProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);

        return new UserNutritionProfileDraft(
            Id: profile.Id,
            DisplayName: profile.DisplayName,
            Sex: profile.Body.Sex,
            AgeYears: profile.Body.AgeYears,
            HeightCm: profile.Body.HeightCm,
            LifestyleActivityLevel: profile.LifestyleActivityLevel
        );
    }

    public static UserNutritionProfile ApplyToProfile(
        UserNutritionProfileDraft draft,
        UserNutritionProfile currentProfile)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(currentProfile);

        var updatedBody = currentProfile.Body with {
            Sex = draft.Sex,
            AgeYears = draft.AgeYears!.Value,
            HeightCm = draft.HeightCm!.Value,
        };

        return currentProfile with {
            DisplayName = draft.DisplayName.Trim(),
            Body = updatedBody,
            LifestyleActivityLevel = draft.LifestyleActivityLevel,
        };
    }
}