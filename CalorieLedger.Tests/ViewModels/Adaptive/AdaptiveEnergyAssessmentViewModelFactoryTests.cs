using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Adaptive;

namespace CalorieLedger.Tests.ViewModels.Adaptive;

public sealed class AdaptiveEnergyAssessmentViewModelFactoryTests {
    [Fact]
    public void Create_Unavailable_ReturnsUnavailableViewModel() {
        var actionInvoked = false;

        var presentation = new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.Unavailable,
            Details: "Недостаточно измерений."
        );

        var viewModel = AdaptiveEnergyAssessmentViewModelFactory.Create(
            presentation,
            openGoalEditor: _ => actionInvoked = true
        );

        Assert.True(viewModel.IsUnavailable);
        Assert.False(viewModel.CanOpenGoalEditor);
        Assert.False(viewModel.OpenGoalEditorCommand.CanExecute(null));
        Assert.False(actionInvoked);
    }

    [Fact]
    public void Create_WithinTarget_ReturnsWithinTargetViewModel() {
        var presentation = new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.WithinTarget,
            Details: "Фактический темп соответствует запланированному."
        );

        var viewModel = AdaptiveEnergyAssessmentViewModelFactory.Create(
            presentation,
            openGoalEditor: _ => { }
        );

        Assert.True(viewModel.IsWithinTarget);
        Assert.False(viewModel.CanOpenGoalEditor);
        Assert.False(viewModel.OpenGoalEditorCommand.CanExecute(null));
    }

    [Fact]
    public void Create_ObservationRequired_ReturnsObservationViewModel() {
        var presentation = new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.ObservationRequired,
            Details: "Получена только одна оценка отклонения.",
            Recommendation: "Продолжите измерения ещё неделю."
        );

        var viewModel = AdaptiveEnergyAssessmentViewModelFactory.Create(
            presentation,
            openGoalEditor: _ => { }
        );

        Assert.True(viewModel.IsObservationRequired);
        Assert.Equal(
            "Продолжите измерения ещё неделю.",
            viewModel.Recommendation
        );

        Assert.False(viewModel.CanOpenGoalEditor);
    }

    [Fact]
    public void Create_AdjustmentSuggested_ConnectsSuggestedStrategyAction() {
        AdaptiveEnergyStrategySuggestion? openedSuggestion = null;

        var suggestion = new AdaptiveEnergyStrategySuggestion(
            Mode: EnergyStrategyMode.BalancePercent,
            Value: 17m
        );

        var presentation = new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.AdjustmentSuggested,
            Details: "Отклонение сохраняется несколько периодов.",
            Recommendation: "Установите дефицит 17%.",
            SuggestedStrategy: suggestion
        );

        var viewModel = AdaptiveEnergyAssessmentViewModelFactory.Create(
            presentation,
            openGoalEditor: value => openedSuggestion = value
        );

        Assert.True(viewModel.IsAdjustmentSuggested);
        Assert.True(viewModel.CanOpenGoalEditor);
        Assert.True(viewModel.OpenGoalEditorCommand.CanExecute(null));

        viewModel.OpenGoalEditorCommand.Execute(null);

        Assert.Equal(
            suggestion,
            openedSuggestion
        );
    }

    [Fact]
    public void Create_AdjustmentWithoutSuggestedStrategy_HidesEditorAction() {
        var presentation = new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.AdjustmentSuggested,
            Details: "Отклонение сохраняется несколько периодов.",
            Recommendation: "Рассмотрите изменение стратегии."
        );

        var viewModel = AdaptiveEnergyAssessmentViewModelFactory.Create(
            presentation,
            openGoalEditor: _ => { }
        );

        Assert.True(viewModel.IsAdjustmentSuggested);
        Assert.False(viewModel.CanOpenGoalEditor);
        Assert.False(viewModel.OpenGoalEditorCommand.CanExecute(null));
    }

    [Fact]
    public void LoadCurrentGoalWithSuggestedStrategy_ReplacesStrategy() {
        var storedGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy: EnergyStrategy.FromBalancePercent(15m)
        );

        var store = new TestUserNutritionProfileStore(storedGoal);

        var updateService = new NutritionGoalUpdateService(store);

        var editorService = new NutritionGoalEditorService(
            profileProvider: store,
            goalUpdateService: updateService
        );

        var draft = editorService.LoadCurrentGoalWithSuggestedStrategy(
            strategyMode: EnergyStrategyMode.WeightChangePerWeek,
            strategyValue: 0.4m
        );

        Assert.Equal(
            WeightGoalType.LoseWeight,
            draft.GoalType
        );

        Assert.Equal(
            EnergyStrategyMode.WeightChangePerWeek,
            draft.StrategyMode
        );

        Assert.Equal(
            0.4m,
            draft.StrategyValue
        );

        Assert.Equal(
            75m,
            draft.TargetWeightKg
        );
    }

    [Fact]
    public void LoadCurrentGoalWithSuggestedStrategy_MaintenanceKeepsNeutralStrategy() {
        var store = new TestUserNutritionProfileStore(
            CreateMaintenanceGoal()
        );

        var updateService = new NutritionGoalUpdateService(store);

        var editorService = new NutritionGoalEditorService(
            profileProvider: store,
            goalUpdateService: updateService
        );

        var draft = editorService.LoadCurrentGoalWithSuggestedStrategy(
            strategyMode: EnergyStrategyMode.BalancePercent,
            strategyValue: 10m
        );

        Assert.Equal(
            WeightGoalType.Maintain,
            draft.GoalType
        );

        Assert.Equal(
            EnergyStrategyMode.BalancePercent,
            draft.StrategyMode
        );

        Assert.Equal(
            0m,
            draft.StrategyValue
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LoadCurrentGoalWithSuggestedStrategy_NonPositiveValue_Throws(int strategyValue) {
        var store = new TestUserNutritionProfileStore(
            CreateMaintenanceGoal()
        );

        var updateService =
        new NutritionGoalUpdateService(store);

        var editorService =
        new NutritionGoalEditorService(
            profileProvider: store,
            goalUpdateService: updateService
        );

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            editorService.LoadCurrentGoalWithSuggestedStrategy(
                strategyMode: EnergyStrategyMode.BalancePercent,
                strategyValue: strategyValue
            )
        );
    }

    [Fact]
    public void LoadCurrentGoalWithSuggestedStrategy_HundredPercent_Throws() {
        var store = new TestUserNutritionProfileStore(
            CreateMaintenanceGoal()
        );

        var updateService = new NutritionGoalUpdateService(store);

        var editorService = new NutritionGoalEditorService(
            profileProvider: store,
            goalUpdateService: updateService
        );

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            editorService.LoadCurrentGoalWithSuggestedStrategy(
                strategyMode: EnergyStrategyMode.BalancePercent,
                strategyValue: 100m
            )
        );
    }

    private static NutritionGoal CreateMaintenanceGoal() {
        return new NutritionGoal(
            GoalType: WeightGoalType.Maintain,
            Strategy: EnergyStrategy.FromBalancePercent(0m)
        );
    }

    private sealed class TestUserNutritionProfileStore:IUserNutritionProfileStore {
        private UserNutritionProfile currentProfile;

        public TestUserNutritionProfileStore(NutritionGoal initialGoal) {
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
                Goal: initialGoal
            );
        }

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