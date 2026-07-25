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
            openGoalEditor: () => actionInvoked = true
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
            openGoalEditor: () => { }
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
            openGoalEditor: () => { }
        );

        Assert.True(viewModel.IsObservationRequired);
        Assert.Equal(
            "Продолжите измерения ещё неделю.",
            viewModel.Recommendation
        );

        Assert.False(viewModel.CanOpenGoalEditor);
    }

    [Fact]
    public void Create_AdjustmentSuggested_ConnectsGoalEditorAction() {
        var actionInvoked = false;

        var presentation = new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.AdjustmentSuggested,
            Details: "Отклонение сохраняется несколько периодов.",
            Recommendation: "Измените энергетическую стратегию."
        );

        var viewModel = AdaptiveEnergyAssessmentViewModelFactory.Create(
            presentation,
            openGoalEditor: () => actionInvoked = true
        );

        Assert.True(viewModel.IsAdjustmentSuggested);
        Assert.True(viewModel.CanOpenGoalEditor);
        Assert.True(viewModel.OpenGoalEditorCommand.CanExecute(null));

        viewModel.OpenGoalEditorCommand.Execute(null);

        Assert.True(actionInvoked);
    }
}