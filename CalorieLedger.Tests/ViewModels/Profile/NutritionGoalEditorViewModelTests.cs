using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class NutritionGoalEditorViewModelTests {
    [Fact]
    public void Constructor_LoadsValuesFromDraft() {
        var store = new TestUserNutritionProfileStore();
        var editorService = CreateEditorService(store);

        var draft = new NutritionGoalDraft(
            GoalType: WeightGoalType.GainWeight,
            TargetWeightKg: 85m,
            TargetMuscleMassKg: 42m,
            EnergyBalancePercent: 5m,
            StopAtBodyFatPercent: 18m,
            MassGainIntent:
                MassGainIntent.LeanMassPriority);

        var viewModel = new NutritionGoalEditorViewModel(
            editorService: editorService,
            draft: draft,
            onSaved: () => { },
            onCancelled: () => { });

        Assert.Equal(
            WeightGoalType.GainWeight,
            viewModel.GoalType);

        Assert.Equal(
            85m,
            viewModel.TargetWeightKg);

        Assert.Equal(
            42m,
            viewModel.TargetMuscleMassKg);

        Assert.Equal(
            5m,
            viewModel.EnergyBalancePercent);

        Assert.Equal(
            18m,
            viewModel.StopAtBodyFatPercent);

        Assert.Equal(
            MassGainIntent.LeanMassPriority,
            viewModel.MassGainIntent);

        Assert.True(viewModel.IsWeightGain);
        Assert.True(viewModel.CanEditMassGainOptions);
    }

    [Fact]
    public void SaveCommand_ValidGoal_UpdatesProfileAndInvokesCallback() {
        var store = new TestUserNutritionProfileStore();
        var editorService = CreateEditorService(store);

        var callbackInvoked = false;

        var draft =
            editorService.CreateNewGoal(
                WeightGoalType.LoseWeight);

        var viewModel = new NutritionGoalEditorViewModel(
            editorService: editorService,
            draft: draft,
            onSaved: () => callbackInvoked = true,
            onCancelled: () => { });

        viewModel.TargetWeightKg = 75m;
        viewModel.TargetBodyFatPercent = 15m;
        viewModel.EnergyBalancePercent = -15m;

        viewModel.SaveCommand.Execute(null);

        Assert.True(callbackInvoked);
        Assert.False(viewModel.HasValidationErrors);
        Assert.Empty(viewModel.ValidationMessages);

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
    public void SaveCommand_InvalidGoal_ShowsErrorsAndDoesNotInvokeCallback() {
        var store = new TestUserNutritionProfileStore();
        var originalGoal =
            store.GetCurrentProfile().Goal;

        var editorService = CreateEditorService(store);
        var callbackInvoked = false;

        var draft =
            editorService.CreateNewGoal(
                WeightGoalType.LoseWeight);

        var viewModel = new NutritionGoalEditorViewModel(
            editorService: editorService,
            draft: draft,
            onSaved: () => callbackInvoked = true,
            onCancelled: () => { });

        viewModel.TargetWeightKg = 75m;
        viewModel.EnergyBalancePercent = 5m;

        viewModel.SaveCommand.Execute(null);

        Assert.False(callbackInvoked);
        Assert.True(viewModel.HasValidationErrors);
        Assert.NotEmpty(viewModel.ValidationMessages);

        Assert.Equal(
            originalGoal,
            store.GetCurrentProfile().Goal);
    }

    [Fact]
    public void SaveCommand_WeightLoss_IgnoresMassGainOnlyFields() {
        var store = new TestUserNutritionProfileStore();
        var editorService = CreateEditorService(store);

        var draft = new NutritionGoalDraft(
            GoalType: WeightGoalType.GainWeight,
            TargetWeightKg: 85m,
            EnergyBalancePercent: 5m,
            StopAtBodyFatPercent: 18m,
            MassGainIntent:
                MassGainIntent.LeanMassPriority);

        var viewModel = new NutritionGoalEditorViewModel(
            editorService: editorService,
            draft: draft,
            onSaved: () => { },
            onCancelled: () => { });

        viewModel.GoalType =
            WeightGoalType.LoseWeight;

        viewModel.TargetWeightKg = 75m;
        viewModel.EnergyBalancePercent = -15m;

        viewModel.SaveCommand.Execute(null);

        var savedGoal =
            store.GetCurrentProfile().Goal;

        Assert.Equal(
            WeightGoalType.LoseWeight,
            savedGoal.GoalType);

        Assert.Null(
            savedGoal.StopAtBodyFatPercent);

        Assert.Null(
            savedGoal.MassGainIntent);
    }

    [Fact]
    public void CancelCommand_InvokesCancellationCallback() {
        var store = new TestUserNutritionProfileStore();
        var editorService = CreateEditorService(store);

        var callbackInvoked = false;

        var draft =
            editorService.LoadCurrentGoal();

        var viewModel = new NutritionGoalEditorViewModel(
            editorService: editorService,
            draft: draft,
            onSaved: () => { },
            onCancelled: () => callbackInvoked = true);

        viewModel.CancelCommand.Execute(null);

        Assert.True(callbackInvoked);
    }

    private static NutritionGoalEditorService
        CreateEditorService(
            IUserNutritionProfileStore store) {
        var updateService =
            new NutritionGoalUpdateService(store);

        return new NutritionGoalEditorService(
            profileProvider: store,
            goalUpdateService: updateService);
    }

    private sealed class TestUserNutritionProfileStore
        :IUserNutritionProfileStore {
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

        public void UpdateGoal(
            NutritionGoal goal) {
            currentProfile = currentProfile with
            {
                Goal = goal
            };
        }
    }
}