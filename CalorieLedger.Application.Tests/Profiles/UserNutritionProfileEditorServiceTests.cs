using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class UserNutritionProfileEditorServiceTests {
    [Fact]
    public void LoadCurrentProfile_ReturnsCurrentProfileDraft() {
        var profile = CreateProfile();

        var store = new InMemoryUserNutritionProfileStore(profile);

        var service = new UserNutritionProfileEditorService(
            profileStore: store,
            profileWriter: store
        );

        var draft = service.LoadCurrentProfile();

        Assert.Equal(profile.Id, draft.Id);
        Assert.Equal(profile.DisplayName, draft.DisplayName);
        Assert.Equal(profile.Body.Sex, draft.Sex);
        Assert.Equal(profile.Body.AgeYears, draft.AgeYears);
        Assert.Equal(profile.Body.HeightCm, draft.HeightCm);

        Assert.Equal(
            profile.LifestyleActivityLevel,
            draft.LifestyleActivityLevel
        );
    }

    [Fact]
    public void Save_ValidDraft_UpdatesEditableFields() {
        var originalProfile = CreateProfile();

        var store = new InMemoryUserNutritionProfileStore(originalProfile);

        var service = new UserNutritionProfileEditorService(
            profileStore: store,
            profileWriter: store
        );

        var draft = new UserNutritionProfileDraft(
            Id: originalProfile.Id,
            DisplayName: "Updated user",
            Sex: BiologicalSex.Female,
            AgeYears: 27,
            HeightCm: 184m,
            LifestyleActivityLevel: LifestyleActivityLevel.VeryActive
        );

        var result = service.Save(draft);
        var savedProfile = store.GetCurrentProfile();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);

        Assert.Equal("Updated user", savedProfile.DisplayName);
        Assert.Equal(BiologicalSex.Female, savedProfile.Body.Sex);
        Assert.Equal(27, savedProfile.Body.AgeYears);
        Assert.Equal(184m, savedProfile.Body.HeightCm);

        Assert.Equal(
            LifestyleActivityLevel.VeryActive,
            savedProfile.LifestyleActivityLevel
        );

        Assert.Equal(
            originalProfile.Body.WeightKg,
            savedProfile.Body.WeightKg
        );

        Assert.Equal(
            originalProfile.Body.BodyFatPercent,
            savedProfile.Body.BodyFatPercent
        );

        Assert.Equal(
            originalProfile.Body.BoneMassKg,
            savedProfile.Body.BoneMassKg
        );

        Assert.Equal(
            originalProfile.Body.MuscleMassKg,
            savedProfile.Body.MuscleMassKg
        );

        Assert.Equal(
            originalProfile.Body.MusclePercent,
            savedProfile.Body.MusclePercent
        );

        Assert.Equal(
            originalProfile.Goal,
            savedProfile.Goal
        );
    }

    [Fact]
    public void Save_InvalidDraft_DoesNotUpdateProfile() {
        var originalProfile = CreateProfile();

        var store = new InMemoryUserNutritionProfileStore(originalProfile);

        var service = new UserNutritionProfileEditorService(
            profileStore: store,
            profileWriter: store
        );

        var invalidDraft = new UserNutritionProfileDraft(
            Id: originalProfile.Id,
            DisplayName: " ",
            Sex: BiologicalSex.Female,
            AgeYears: 0,
            HeightCm: 300m,
            LifestyleActivityLevel: LifestyleActivityLevel.VeryActive
        );

        var result = service.Save(invalidDraft);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            UserNutritionProfileValidationError.MissingDisplayName,
            result.Errors
        );

        Assert.Contains(
            UserNutritionProfileValidationError.InvalidAge,
            result.Errors
        );

        Assert.Contains(
            UserNutritionProfileValidationError.InvalidHeight,
            result.Errors
        );

        Assert.Equal(
            originalProfile,
            store.GetCurrentProfile()
        );
    }

    [Fact]
    public void Save_DifferentProfileId_DoesNotUpdateProfile() {
        var originalProfile = CreateProfile();

        var store = new InMemoryUserNutritionProfileStore(originalProfile);

        var service = new UserNutritionProfileEditorService(
            profileStore: store,
            profileWriter: store
        );

        var draft = UserNutritionProfileDraftMapper.FromProfile(originalProfile) with {
            Id = Guid.NewGuid(),
        };

        var result = service.Save(draft);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            UserNutritionProfileValidationError.ProfileIdMismatch,
            result.Errors
        );

        Assert.Equal(
            originalProfile,
            store.GetCurrentProfile()
        );
    }

    private static UserNutritionProfile CreateProfile() {
        return new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Test user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Male,
                AgeYears: 30,
                HeightCm: 180m,
                WeightKg: 80m,
                BodyFatPercent: 20m,
                BoneMassKg: 3.2m,
                MuscleMassKg: 35m,
                MusclePercent: 43.75m
            ),
            LifestyleActivityLevel: LifestyleActivityLevel.Sedentary,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.LoseWeight,
                TargetWeightKg: 75m,
                Strategy: EnergyStrategy.FromBalancePercent(15m)
            )
        );
    }
}