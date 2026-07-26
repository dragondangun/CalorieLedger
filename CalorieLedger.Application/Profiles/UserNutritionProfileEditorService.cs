using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class UserNutritionProfileEditorService {
    private readonly IUserNutritionProfileStore profileStore;
    private readonly IUserNutritionProfileWriter profileWriter;

    public UserNutritionProfileEditorService(
        IUserNutritionProfileStore profileStore,
        IUserNutritionProfileWriter profileWriter) {
        ArgumentNullException.ThrowIfNull(profileStore);
        ArgumentNullException.ThrowIfNull(profileWriter);

        this.profileStore = profileStore;
        this.profileWriter = profileWriter;
    }

    public UserNutritionProfileDraft LoadCurrentProfile() {
        var profile = profileStore.GetCurrentProfile();

        return UserNutritionProfileDraftMapper.FromProfile(profile);
    }

    public UserNutritionProfileSaveResult Save(UserNutritionProfileDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        var currentProfile = profileStore.GetCurrentProfile();
        var errors = Validate(draft, currentProfile);

        if(errors.Count > 0) {
            return new UserNutritionProfileSaveResult(
                IsSuccess: false,
                Errors: errors
            );
        }

        var updatedProfile = UserNutritionProfileDraftMapper.ApplyToProfile(
            draft,
            currentProfile
        );

        profileWriter.UpdateProfile(updatedProfile);

        return new UserNutritionProfileSaveResult(
            IsSuccess: true,
            Errors: []
        );
    }

    private static IReadOnlyList<UserNutritionProfileValidationError> Validate(
        UserNutritionProfileDraft draft,
        UserNutritionProfile currentProfile)
    {
        var errors = new List<UserNutritionProfileValidationError>();

        if(draft.Id == Guid.Empty) {
            errors.Add(UserNutritionProfileValidationError.MissingId);
        }
        else if(draft.Id != currentProfile.Id) {
            errors.Add(UserNutritionProfileValidationError.ProfileIdMismatch);
        }

        if(string.IsNullOrWhiteSpace(draft.DisplayName)) {
            errors.Add(UserNutritionProfileValidationError.MissingDisplayName);
        }

        if(!Enum.IsDefined(typeof(BiologicalSex), draft.Sex)) {
            errors.Add(UserNutritionProfileValidationError.InvalidSex);
        }

        if(draft.AgeYears is null or < 1 or > 120) {
            errors.Add(UserNutritionProfileValidationError.InvalidAge);
        }

        if(draft.HeightCm is null or < 50m or > 250m) {
            errors.Add(UserNutritionProfileValidationError.InvalidHeight);
        }

        if(!Enum.IsDefined(typeof(LifestyleActivityLevel), draft.LifestyleActivityLevel)) {
            errors.Add(UserNutritionProfileValidationError.InvalidLifestyleActivityLevel);
        }

        return errors;
    }
}