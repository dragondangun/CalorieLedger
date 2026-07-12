using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class NutritionGoalEditorServiceTests {
    [Fact]
    public void LoadCurrentGoal_ReturnsDraftFromStoredGoal() {
        // Legacy goal: verifies temporary backward-compatible reading.
        var storedGoal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            TargetWeightKg: 85m,
            TargetMuscleMassKg: 42m,
            Strategy: EnergyStrategy.FromBalancePercent(5m),
            StopAtBodyFatPercent: 18m,
            MassGainIntent: MassGainIntent.LeanMassPriority);

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
            EnergyStrategyMode.BalancePercent,
            draft.StrategyMode);

        Assert.Equal(
            5m,
            draft.StrategyValue);

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
            EnergyStrategyMode.BalancePercent,
            draft.StrategyMode);

        Assert.Equal(
            0m,
            draft.StrategyValue);

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
            StrategyMode:
                EnergyStrategyMode.BalancePercent,
            StrategyValue: 15m);

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

        Assert.NotNull(savedGoal.Strategy);

        Assert.Equal(
            EnergyStrategyMode.BalancePercent,
            savedGoal.Strategy.Mode);

        Assert.Equal(
            15m,
            savedGoal.Strategy.Value);
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
                StrategyMode:
                    EnergyStrategyMode.BalancePercent,
                StrategyValue: 0m);

        var result =
            editorService.Save(invalidDraft);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            NutritionGoalValidationError
                .InvalidEnergyStrategyValue,
            result.Errors);

        Assert.Equal(
            originalGoal,
            store.GetCurrentProfile().Goal);
    }

    [Fact]
    public void LoadCurrentGoal_UnifiedDeficitStrategy_ReturnsUnifiedDraftStrategy() {
        var storedGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy:
                EnergyStrategy.FromBalancePercent(15m));

        var store = new TestUserNutritionProfileStore(
                storedGoal);

        var updateService = new NutritionGoalUpdateService(store);

        var editorService = new NutritionGoalEditorService(
            store,
            updateService);

        var draft = editorService.LoadCurrentGoal();

        Assert.Equal(
            EnergyStrategyMode.BalancePercent,
            draft.StrategyMode);

        Assert.Equal(
            15m,
            draft.StrategyValue);
    }

    private static NutritionGoal CreateMaintenanceGoal() {
        return new NutritionGoal(
            GoalType: WeightGoalType.Maintain,
            Strategy:
                EnergyStrategy.FromBalancePercent(0m));
    }

    private sealed class TestUserNutritionProfileStore:IUserNutritionProfileStore {
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

        public UserNutritionProfile GetCurrentProfile() {
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

    [Fact]
    public void CalculateStrategyPreview_PercentDeficit_ReturnsWeightLossForecast() {
        var store = new TestUserNutritionProfileStore(
            CreateMaintenanceGoal());

        var updateService = new NutritionGoalUpdateService(store);

        var editorService = new NutritionGoalEditorService(
            store,
            updateService);

        var draft = new NutritionGoalDraft(
            GoalType: WeightGoalType.LoseWeight,
            StrategyMode: EnergyStrategyMode.BalancePercent,
            StrategyValue: 15m);

        var preview = editorService.CalculateStrategyPreview(
            draft);

        Assert.NotNull(preview);

        Assert.Equal(
            -15m,
            preview.EnergyBalancePercent);

        Assert.True(
            preview.PredictedWeightChangeKgPerWeek
            < 0m);

        Assert.True(
            preview.TargetCaloriesKcal
            < preview.MaintenanceCaloriesKcal);
    }

    [Fact]
    public void CalculateStrategyPreview_ZeroWeightLossStrategy_ReturnsNull() {
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
        StrategyMode:
            EnergyStrategyMode.BalancePercent,
        StrategyValue: 0m);

        var preview =
        editorService.CalculateStrategyPreview(
            draft);

        Assert.Null(preview);
    }

    [Fact]
    public void CalculateStrategyPreview_ZeroMaintenanceStrategy_ReturnsNeutralPreview() {
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
        GoalType: WeightGoalType.Maintain,
        StrategyMode:
            EnergyStrategyMode.BalancePercent,
        StrategyValue: 0m);

        var preview =
        editorService.CalculateStrategyPreview(
            draft);

        Assert.NotNull(preview);

        Assert.Equal(
            0m,
            preview.EnergyBalancePercent);

        Assert.Equal(
            0m,
            preview.PredictedWeightChangeKgPerWeek);

        Assert.Equal(
            preview.MaintenanceCaloriesKcal,
            preview.TargetCaloriesKcal);
    }
}