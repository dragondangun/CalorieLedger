using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class SampleUserNutritionProfileProvider:IUserNutritionProfileProvider {
    public UserNutritionProfile GetCurrentProfile() {
        return new UserNutritionProfile(
            Id: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            DisplayName: "Test user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Male,
                AgeYears: 30,
                HeightCm: 180m,
                WeightKg: 80m,
                BodyFatPercent: null,
                BoneMassKg: null),
            LifestyleActivityLevel: LifestyleActivityLevel.Sedentary,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain));
    }
}