using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class NutritionGoalEditorServiceTests {
    [Fact]
    public void LoadCurrentGoal_ReturnsDraftFromStoredGoal() {
        var storedGoal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            TargetWeightKg: 85m,
            TargetMuscleMassKg: 42m,
            EnergyBalancePercent: 5m,
            StopAtBodyFatPercent: 18m,
            MassGainIntent:
                MassGainIntent.LeanMassPriority);

        var store =
            new TestUserNutritionProfileStore(storedGoal);

        var updateService =
            new NutritionGoalUpdateService(store);

        var editorService =
            new NutritionGoalEditorService(
                store,
                updateService);

        var draft =
            editorService.LoadCurrentGoal();

        Assert.Equal(
            storedGoal.GoalType,
            draft.GoalType);

        Assert.Equal(
            storedGoal.TargetWeightKg,
            draft.TargetWeightKg);

        Assert.Equal(
            storedGoal.TargetMuscleMassKg,
            draft.TargetMuscleMassKg);

        Assert.Equal(
            storedGoal.EnergyBalancePercent,
            draft.EnergyBalancePercent);

        Assert.Equal(
            storedGoal.StopAtBodyFatPercent,
            draft.StopAtBodyFatPercent);

        Assert.Equal(
            storedGoal.MassGainIntent,
            draft.MassGainIntent);
    }

    [Fact]
    public void CreateNewGoal_Maintenance_SetsNeutralEnergyBalance() {
        var store =
            new TestUserNutritionProfileStore(
                CreateMaintenanceGoal());

        var updateService =
            new NutritionGoalUpdateService(store);

        var editorService =
            new NutritionGoalEditorService(
                store,
                updateService);

        var draft =
            editorService.CreateNewGoal(
                WeightGoalType.Maintain);

        Assert.Equal(
            WeightGoalType.Maintain,
            draft.GoalType);

        Assert.Equal(
            0m,
            draft.EnergyBalancePercent);

        Assert.Null(
            draft.TargetWeightKg);
    }

    [Fact]
    public void Save_ValidDraft_UpdatesStoredGoal() {
        var store =
            new TestUserNutritionProfileStore(
                CreateMaintenanceGoal());

        var updateService =
            new NutritionGoalUpdateService(store);

        var editorService =
            new NutritionGoalEditorService(
                store,
                updateService);

        var draft = new NutritionGoalDraft(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m,
            EnergyBalancePercent: -15m);

        var result =
            editorService.Save(draft);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);

        var savedGoal =
            store.GetCurrentProfile().Goal;

        Assert.Equal(
            WeightGoalType.LoseWeight,
            savedGoal.GoalType);

        Assert.Equal(
            75m,
            savedGoal.TargetWeightKg);

        Assert.Equal(
            15m,
            savedGoal.TargetBodyFatPercent);

        Assert.Equal(
            -15m,
            savedGoal.EnergyBalancePercent);
    }

    [Fact]
    public void Save_InvalidDraft_DoesNotUpdateStoredGoal() {
        var originalGoal =
            CreateMaintenanceGoal();

        var store =
            new TestUserNutritionProfileStore(
                originalGoal);

        var updateService =
            new NutritionGoalUpdateService(store);

        var editorService =
            new NutritionGoalEditorService(
                store,
                updateService);

        var invalidDraft =
            new NutritionGoalDraft(
                GoalType:
                    WeightGoalType.LoseWeight,
                TargetWeightKg: 75m,
                EnergyBalancePercent: 5m);

        var result =
            editorService.Save(invalidDraft);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            NutritionGoalValidationError
                .WeightLossRequiresDeficit,
            result.Errors);

        Assert.Equal(
            originalGoal,
            store.GetCurrentProfile().Goal);
    }

    private static NutritionGoal
        CreateMaintenanceGoal() {
        return new NutritionGoal(
            GoalType: WeightGoalType.Maintain,
            EnergyBalancePercent: 0m);
    }

    private sealed class TestUserNutritionProfileStore
        :IUserNutritionProfileStore {
        private UserNutritionProfile currentProfile;

        public TestUserNutritionProfileStore(
            NutritionGoal initialGoal) {
            currentProfile =
                new UserNutritionProfile(
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
                    Goal: initialGoal);
        }

        public UserNutritionProfile
            GetCurrentProfile() {
            return currentProfile;
        }

        public void UpdateGoal(
            NutritionGoal goal) {
            currentProfile = currentProfile with
            {
                Goal = goal
            };
        }
    }
}