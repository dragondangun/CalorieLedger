using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class InMemoryUserNutritionProfileStoreTests {
    [Fact]
    public void GetCurrentProfile_ReturnsInitialProfile() {
        var initialProfile = CreateProfile();

        var store = new InMemoryUserNutritionProfileStore(
            initialProfile
        );

        var result = store.GetCurrentProfile();

        Assert.Equal(
            initialProfile,
            result
        );
    }

    [Fact]
    public void UpdateGoal_ReplacesOnlyGoal() {
        var initialProfile = CreateProfile();

        var store = new InMemoryUserNutritionProfileStore(
            initialProfile
        );

        var updatedGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy: EnergyStrategy.FromBalancePercent(15m)
        );

        store.UpdateGoal(updatedGoal);

        var result = store.GetCurrentProfile();

        Assert.Equal(
            updatedGoal,
            result.Goal
        );

        Assert.Equal(
            initialProfile.Id,
            result.Id
        );

        Assert.Equal(
            initialProfile.DisplayName,
            result.DisplayName
        );

        Assert.Equal(
            initialProfile.Body,
            result.Body
        );

        Assert.Equal(
            initialProfile.LifestyleActivityLevel,
            result.LifestyleActivityLevel
        );
    }

    [Fact]
    public void UpdateProfile_ReplacesCompleteProfile() {
        var store = new InMemoryUserNutritionProfileStore(
            CreateProfile()
        );

        var updatedProfile = CreateProfile() with {
            DisplayName = "Updated user",
            LifestyleActivityLevel = LifestyleActivityLevel.VeryActive,
            Body = new BodyProfile(
                Sex: BiologicalSex.Female,
                AgeYears: 27,
                HeightCm: 184m,
                WeightKg: 70m
            ),
        };

        store.UpdateProfile(updatedProfile);

        Assert.Equal(
            updatedProfile,
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
                GoalType: WeightGoalType.Maintain,
                Strategy: EnergyStrategy.FromBalancePercent(0m)
            )
        );
    }
}