using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class SampleUserNutritionProfileProvider
    :IUserNutritionProfileStore {
    private UserNutritionProfile currentProfile =
        CreateInitialProfile();

    public UserNutritionProfile GetCurrentProfile() {
        return currentProfile;
    }

    public void UpdateGoal(NutritionGoal goal) {
        currentProfile = currentProfile with
        {
            Goal = goal
        };
    }

    private static UserNutritionProfile CreateInitialProfile() {
        return new UserNutritionProfile(
            Id: Guid.Parse(
                "00000000-0000-0000-0000-000000000001"),
            DisplayName: "Test user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Male,
                AgeYears: 30,
                HeightCm: 180m,
                WeightKg: 75m,
                BodyFatPercent: 15m,
                BoneMassKg: null,
                MuscleMassKg: null,
                MusclePercent: null),
            LifestyleActivityLevel:
                LifestyleActivityLevel.Sedentary,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.LoseWeight,
                TargetWeightKg: 75m,
                TargetBodyFatPercent: 15m,
                EnergyBalancePercent: -15m));
    }
}