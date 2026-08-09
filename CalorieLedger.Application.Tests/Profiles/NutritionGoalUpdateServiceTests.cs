using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CalorieLedger.Application.Adaptive;

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
            Strategy: EnergyStrategy.FromBalancePercent(15m)
        );

        var result = service.UpdateGoal(goal);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(
            goal,
            store.GetCurrentProfile().Goal
        );
    }

    [Fact]
    public void UpdateGoal_InvalidGoal_DoesNotUpdateStoredProfile() {
        var store = new TestUserNutritionProfileStore();
        var service = new NutritionGoalUpdateService(store);

        var originalGoal = store.GetCurrentProfile().Goal;

        var invalidGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy: EnergyStrategy.FromBalancePercent(0m)
        );

        var result = service.UpdateGoal(invalidGoal);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            NutritionGoalValidationError.InvalidEnergyStrategyValue,
            result.Errors
        );

        Assert.Equal(
            originalGoal,
            store.GetCurrentProfile().Goal
        );
    }

    private sealed class TestUserNutritionProfileStore:IUserNutritionProfileStore {
        private UserNutritionProfile currentProfile;

        public TestUserNutritionProfileStore(
            NutritionGoal? goal = null
        ) {
            currentProfile = new UserNutritionProfile(
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
                    MusclePercent: null
                ),
                LifestyleActivityLevel: LifestyleActivityLevel.Sedentary,
                Goal: goal ?? new NutritionGoal(
                    GoalType: WeightGoalType.Maintain,
                    Strategy: EnergyStrategy.FromBalancePercent(0m)
                )
            );
        }

        public UserNutritionProfile GetCurrentProfile() {
            return currentProfile;
        }

        public void UpdateGoal(NutritionGoal goal) {
            currentProfile = currentProfile with {
                Goal = goal,
            };
        }
    }

    [Fact]
    public void UpdateGoal_MissingStrategy_DoesNotUpdateStoredProfile() {
        var store = new TestUserNutritionProfileStore();
        var service = new NutritionGoalUpdateService(store);

        var originalGoal = store.GetCurrentProfile().Goal;

        var invalidGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy: null
        );

        var result = service.UpdateGoal(invalidGoal);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            NutritionGoalValidationError.MissingEnergyStrategy,
            result.Errors
        );

        Assert.Equal(
            originalGoal,
            store.GetCurrentProfile().Goal
        );
    }

    private sealed class TestAdaptiveEnergyHistoryResetter:IAdaptiveEnergyHistoryResetter {
        public int ResetCallCount { get; private set; }

        public void ResetHistory() {
            ResetCallCount++;
        }
    }

    [Fact]
    public void UpdateGoal_EnergyConfigurationChanged_ResetsAdaptiveHistory() {
        var store = new TestUserNutritionProfileStore();

        var historyResetter = new TrackingAdaptiveEnergyHistoryResetter();

        var service = new NutritionGoalUpdateService(
            store,
            historyResetter
        );

        var goal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m,
            Strategy: EnergyStrategy.FromBalancePercent(15m)
        );

        var result = service.UpdateGoal(
            goal
        );

        Assert.True(result.IsSuccess);

        Assert.Equal(
            1,
            historyResetter.ResetCount
        );
    }

    [Fact]
    public void UpdateGoal_OnlyTargetValuesChanged_DoesNotResetAdaptiveHistory() {
        var initialGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m,
            Strategy: EnergyStrategy.FromBalancePercent(15m)
        );

        var store = new TestUserNutritionProfileStore(
            initialGoal
        );

        var historyResetter = new TrackingAdaptiveEnergyHistoryResetter();

        var service = new NutritionGoalUpdateService(
            store,
            historyResetter
        );

        var updatedGoal = initialGoal with {
            TargetWeightKg = 74m,
            TargetBodyFatPercent = 14m,
        };

        var result = service.UpdateGoal(
            updatedGoal
        );

        Assert.True(result.IsSuccess);

        Assert.Equal(
            0,
            historyResetter.ResetCount
        );
    }

    [Fact]
    public void UpdateGoal_InvalidGoal_DoesNotResetAdaptiveHistory() {
        var store = new TestUserNutritionProfileStore();

        var historyResetter = new TrackingAdaptiveEnergyHistoryResetter();

        var service = new NutritionGoalUpdateService(
            store,
            historyResetter
        );

        var invalidGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy: EnergyStrategy.FromBalancePercent(0m)
        );

        var result = service.UpdateGoal(
            invalidGoal
        );

        Assert.False(result.IsSuccess);

        Assert.Equal(
            0,
            historyResetter.ResetCount
        );
    }

    private sealed class TrackingAdaptiveEnergyHistoryResetter:IAdaptiveEnergyHistoryResetter {
        public int ResetCount { get; private set; }

        public void ResetHistory() {
            ResetCount++;
        }
    }
}
