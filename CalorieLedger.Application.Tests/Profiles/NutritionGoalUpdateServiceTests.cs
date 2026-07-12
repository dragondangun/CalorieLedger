using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class NutritionGoalUpdateServiceTests {
    [Fact]
    public void UpdateGoal_ValidGoal_UpdatesStoredProfile() {
        var store = new TestUserNutritionProfileStore();
        var service = new NutritionGoalUpdateService(store);

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m,
            EnergyBalancePercent: -15m);

        var result = service.UpdateGoal(goal);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(
            goal,
            store.GetCurrentProfile().Goal);
    }

    [Fact]
    public void UpdateGoal_InvalidGoal_DoesNotUpdateStoredProfile() {
        var store = new TestUserNutritionProfileStore();
        var service = new NutritionGoalUpdateService(store);

        var originalGoal =
            store.GetCurrentProfile().Goal;

        var invalidGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            EnergyBalancePercent: 5m);

        var result =
            service.UpdateGoal(invalidGoal);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            NutritionGoalValidationError
                .WeightLossRequiresDeficit,
            result.Errors);

        Assert.Equal(
            originalGoal,
            store.GetCurrentProfile().Goal);
    }

    private sealed class TestUserNutritionProfileStore:IUserNutritionProfileStore {
        private UserNutritionProfile currentProfile =
            new(
                Id: Guid.NewGuid(),
                DisplayName: "Test user",
                Body: new BodyProfile(
                    Sex: BiologicalSex.Male,
                    AgeYears: 30,
                    HeightCm: 180m,
                    WeightKg: 80m,
                    BodyFatPercent: 20m,
                    BoneMassKg: null,
                    MuscleMassKg: null,
                    MusclePercent: null),
                LifestyleActivityLevel:
                    LifestyleActivityLevel.Sedentary,
                Goal: new NutritionGoal(
                    GoalType: WeightGoalType.Maintain,
                    EnergyBalancePercent: 0m));

        public UserNutritionProfile GetCurrentProfile() {
            return currentProfile;
        }

        public void UpdateGoal(NutritionGoal goal) {
            currentProfile = currentProfile with
            {
                Goal = goal
            };
        }
    }
}