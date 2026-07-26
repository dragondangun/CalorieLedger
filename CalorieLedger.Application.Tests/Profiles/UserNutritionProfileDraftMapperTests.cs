using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class UserNutritionProfileDraftMapperTests {
    [Fact]
    public void FromProfile_MapsEditableFields() {
        var profile = CreateProfile();

        var draft = UserNutritionProfileDraftMapper.FromProfile(
            profile
        );

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
    public void ApplyToProfile_PreservesMeasurementsAndGoal() {
        var profile = CreateProfile();

        var draft = new UserNutritionProfileDraft(
            Id: profile.Id,
            DisplayName: " Updated user ",
            Sex: BiologicalSex.Female,
            AgeYears: 27,
            HeightCm: 184m,
            LifestyleActivityLevel: LifestyleActivityLevel.ModeratelyActive
        );

        var result = UserNutritionProfileDraftMapper.ApplyToProfile(
            draft,
            profile
        );

        Assert.Equal("Updated user", result.DisplayName);
        Assert.Equal(BiologicalSex.Female, result.Body.Sex);
        Assert.Equal(27, result.Body.AgeYears);
        Assert.Equal(184m, result.Body.HeightCm);

        Assert.Equal(
            LifestyleActivityLevel.ModeratelyActive,
            result.LifestyleActivityLevel
        );

        Assert.Equal(profile.Body.WeightKg, result.Body.WeightKg);
        Assert.Equal(profile.Body.BodyFatPercent, result.Body.BodyFatPercent);
        Assert.Equal(profile.Body.BoneMassKg, result.Body.BoneMassKg);
        Assert.Equal(profile.Body.MuscleMassKg, result.Body.MuscleMassKg);
        Assert.Equal(profile.Body.MusclePercent, result.Body.MusclePercent);
        Assert.Equal(profile.Goal, result.Goal);
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