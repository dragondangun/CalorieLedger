using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Adaptive;
using CalorieLedger.ViewModels.Profile;
namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelGoalEditorTests {
    [Fact]
    public void SetNewGoalAction_OpensGoalEditor() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

        var action = viewModel.Today.GoalActions.Single(
            x => x.Action == GoalNextAction.SetNewGoal);

        action.SelectCommand.Execute(null);

        Assert.True(viewModel.IsGoalEditorOpen);
        Assert.False(viewModel.IsTodayDashboardVisible);
        Assert.NotNull(viewModel.GoalEditor);
    }

    [Fact]
    public void CancelGoalEditing_ReturnsToTodayDashboard() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

        var action = viewModel.Today.GoalActions.Single(x => x.Action == GoalNextAction.SetNewGoal);

        action.SelectCommand.Execute(null);

        Assert.NotNull(viewModel.GoalEditor);

        viewModel.GoalEditor.CancelCommand.Execute(null);

        Assert.False(viewModel.IsGoalEditorOpen);
        Assert.True(viewModel.IsTodayDashboardVisible);
        Assert.Null(viewModel.GoalEditor);
    }

    [Fact]
    public void SaveValidGoal_ClosesEditorAndRefreshesDashboard() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

        var action = viewModel.Today.GoalActions.Single(x => x.Action == GoalNextAction.SetNewGoal);

        action.SelectCommand.Execute(null);

        var editor = Assert.IsType<NutritionGoalEditorViewModel>(viewModel.GoalEditor);

        editor.GoalType = WeightGoalType.LoseWeight;

        editor.TargetWeightKg = 75m;
        editor.TargetBodyFatPercent = 15m;

        editor.StrategyMode = EnergyStrategyMode.BalancePercent;

        editor.StrategyValue = 10m;

        editor.SaveCommand.Execute(null);

        Assert.False(editor.HasValidationErrors);
        Assert.Empty(editor.ValidationMessages);

        Assert.False(viewModel.IsGoalEditorOpen);
        Assert.True(viewModel.IsTodayDashboardVisible);
        Assert.Null(viewModel.GoalEditor);

        Assert.Contains(
            "Цель сохранена",
            viewModel.Today.GoalActionSelectionSummary);
    }

    [Fact]
    public void SetNewGoalAction_RaisesVisibilityPropertyChangedEvents() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

        var changedProperties = new List<string>();

        viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName is not null) {
                changedProperties.Add(args.PropertyName);
            }
        };

        var action = viewModel.Today.GoalActions.Single(x => x.Action == GoalNextAction.SetNewGoal);

        action.SelectCommand.Execute(null);

        Assert.Contains(
            nameof(MainViewModel.GoalEditor),
            changedProperties);

        Assert.Contains(
            nameof(MainViewModel.IsGoalEditorOpen),
            changedProperties);

        Assert.Contains(
            nameof(MainViewModel.IsTodayDashboardVisible),
            changedProperties);
    }

    [Fact]
    public void SaveInvalidGoal_KeepsEditorOpen() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

        var action = viewModel.Today.GoalActions.Single(x => x.Action == GoalNextAction.SetNewGoal);

        action.SelectCommand.Execute(null);

        var editor = Assert.IsType<CalorieLedger.ViewModels.Profile.NutritionGoalEditorViewModel>(viewModel.GoalEditor);

        editor.GoalType = WeightGoalType.LoseWeight;

        editor.TargetWeightKg = 75m;
        editor.StrategyMode = EnergyStrategyMode.BalancePercent;
        editor.StrategyValue = 0m;

        editor.SaveCommand.Execute(null);

        Assert.True(editor.HasValidationErrors);
        Assert.NotEmpty(editor.ValidationMessages);

        Assert.True(viewModel.IsGoalEditorOpen);
        Assert.False(viewModel.IsTodayDashboardVisible);
        Assert.Same(editor, viewModel.GoalEditor);
    }

    [Fact]
    public void SetNewGoalAction_OpensCurrentGoalDraft() {
        var viewModel = new MainViewModel(
            new InMemoryBodyMeasurementStore()
        );

        var action = viewModel.Today.GoalActions.Single(
            item => item.Action == GoalNextAction.SetNewGoal
        );

        action.SelectCommand.Execute(null);

        Assert.NotNull(viewModel.GoalEditor);

        var expectedGoalType = new SampleUserNutritionProfileProvider()
            .GetCurrentProfile()
            .Goal
            .GoalType;

        Assert.Equal(
            expectedGoalType,
            viewModel.GoalEditor.GoalType
        );
    }

    [Fact]
    public void AdaptiveRecommendation_OpensGoalEditorWithSuggestedStrategy() {
        var presentation = new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.AdjustmentSuggested,
            Details: "Темп снижения веса ниже запланированного.",
            Recommendation: "Установите дефицит 17%.",
            SuggestedStrategy: new AdaptiveEnergyStrategySuggestion(
                Mode: EnergyStrategyMode.BalancePercent,
                Value: 17m
            )
        );

        var viewModel =
        new MainViewModel(
            new InMemoryBodyMeasurementStore(),
            new TestAdaptiveEnergyAssessmentPresentationProvider(
                presentation
            )
        );

        Assert.True(viewModel.AdaptiveEnergyAssessment.IsAdjustmentSuggested);

        Assert.True(viewModel.AdaptiveEnergyAssessment.CanOpenGoalEditor);

        viewModel.AdaptiveEnergyAssessment.OpenGoalEditorCommand.Execute(null);

        var editor = Assert.IsType<NutritionGoalEditorViewModel>(viewModel.GoalEditor);

        Assert.Equal(
            EnergyStrategyMode.BalancePercent,
            editor.StrategyMode
        );

        Assert.Equal(
            17m,
            editor.StrategyValue
        );
    }

    [Fact]
    public void DefaultAdaptiveProvider_ShowsUnavailableAssessment() {
        var viewModel = new MainViewModel(
            new InMemoryBodyMeasurementStore()
        );

        Assert.True(viewModel.AdaptiveEnergyAssessment.IsUnavailable);

        Assert.False(viewModel.AdaptiveEnergyAssessment.CanOpenGoalEditor);
    }

    [Fact]
    public void Constructor_UsesInjectedPersistentProfile() {
        var profile = new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Test user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Female,
                AgeYears: 27,
                HeightCm: 184m,
                WeightKg: 80m
            ),
            LifestyleActivityLevel: LifestyleActivityLevel.Sedentary,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.LoseWeight,
                TargetWeightKg: 75m,
                Strategy:
                    EnergyStrategy.FromBalancePercent(17m)
            )
        );

        var profileStore = new TestUserNutritionProfileStore(profile);

        var viewModel = new MainViewModel(
            new InMemoryBodyMeasurementStore(),
            profileStore,
            new UnavailableAdaptiveEnergyAssessmentPresentationProvider()
        );

        var action = viewModel.Today.GoalActions.Single(
            item => item.Action == GoalNextAction.SetNewGoal
        );

        action.SelectCommand.Execute(null);

        Assert.NotNull(viewModel.GoalEditor);

        Assert.Equal(
            WeightGoalType.LoseWeight,
            viewModel.GoalEditor.GoalType
        );

        Assert.Equal(
            EnergyStrategyMode.BalancePercent,
            viewModel.GoalEditor.StrategyMode
        );

        Assert.Equal(
            17m,
            viewModel.GoalEditor.StrategyValue
        );
    }

    private sealed class TestAdaptiveEnergyAssessmentPresentationProvider:IAdaptiveEnergyAssessmentPresentationProvider {
        private readonly AdaptiveEnergyAssessmentPresentation presentation;

        public TestAdaptiveEnergyAssessmentPresentationProvider(
            AdaptiveEnergyAssessmentPresentation presentation) {
            this.presentation = presentation;
        }

        public AdaptiveEnergyAssessmentPresentation GetCurrent() {
            return presentation;
        }
    }

    private sealed class TestUserNutritionProfileStore:IUserNutritionProfileStore, IUserNutritionProfileWriter {
        private UserNutritionProfile currentProfile;

        public TestUserNutritionProfileStore(UserNutritionProfile initialProfile) {
            currentProfile = initialProfile;
        }

        public UserNutritionProfile GetCurrentProfile() {
            return currentProfile;
        }

        public void UpdateGoal(NutritionGoal goal) {
            currentProfile = currentProfile with {
                Goal = goal,
            };
        }

        public void UpdateProfile(UserNutritionProfile profile) {
            ArgumentNullException.ThrowIfNull(profile);

            currentProfile = profile;
        }
    }
}