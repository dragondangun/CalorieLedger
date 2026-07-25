using CalorieLedger.ViewModels.Adaptive;

namespace CalorieLedger.Tests.ViewModels.Adaptive;

public sealed class AdaptiveEnergyAssessmentViewModelTests {
    [Fact]
    public void CreateUnavailable_CreatesUnavailableCard() {
        var viewModel = AdaptiveEnergyAssessmentViewModel.CreateUnavailable("Недостаточно измерений.");

        Assert.Equal(
            AdaptiveEnergyAssessmentState.Unavailable,
            viewModel.State
        );

        Assert.True(viewModel.IsUnavailable);

        Assert.False(viewModel.IsAvailable);

        Assert.False(viewModel.HasRecommendation);

        Assert.Equal(
            "Пока недостаточно данных",
            viewModel.Summary
        );
    }

    [Fact]
    public void CreateWithinTarget_CreatesAvailableCard() {
        var viewModel = AdaptiveEnergyAssessmentViewModel.CreateWithinTarget("Фактический темп близок к запланированному.");

        Assert.Equal(
            AdaptiveEnergyAssessmentState.WithinTarget,
            viewModel.State
        );

        Assert.True(viewModel.IsAvailable);

        Assert.True(viewModel.IsWithinTarget);

        Assert.True(viewModel.HasRecommendation);

        Assert.Contains("сохранить", viewModel.Recommendation);
    }

    [Fact]
    public void CreateObservationRequired_CreatesObservationCard() {
        var viewModel = AdaptiveEnergyAssessmentViewModel.CreateObservationRequired(
            details: "Обнаружено одно отклонение.",
            recommendation: "Продолжите измерения ещё неделю."
        );

        Assert.Equal(
            AdaptiveEnergyAssessmentState.ObservationRequired,
            viewModel.State
        );

        Assert.True(viewModel.IsObservationRequired);

        Assert.False(viewModel.IsAdjustmentSuggested);

        Assert.Equal(
            "Продолжите измерения ещё неделю.",
            viewModel.Recommendation
        );
    }

    [Fact]
    public void CreateAdjustmentSuggested_CreatesRecommendationCard() {
        var viewModel = AdaptiveEnergyAssessmentViewModel.CreateAdjustmentSuggested(
            details: "Отклонение сохраняется несколько периодов.",
            recommendation: "Уменьшите норму на 100 ккал."
        );

        Assert.Equal(
            AdaptiveEnergyAssessmentState.AdjustmentSuggested,
            viewModel.State
        );

        Assert.True(viewModel.IsAdjustmentSuggested);

        Assert.True(viewModel.HasRecommendation);

        Assert.Contains(
            "100 ккал",
            viewModel.Recommendation
        );
    }

    [Fact]
    public void CreateAdjustmentSuggested_WithoutAction_CannotOpenGoalEditor() {
        var viewModel =
        AdaptiveEnergyAssessmentViewModel.CreateAdjustmentSuggested(
            details: "Отклонение сохраняется несколько периодов.",
            recommendation: "Уменьшите норму на 100 ккал."
        );

        Assert.False(viewModel.CanOpenGoalEditor);
        Assert.False(viewModel.OpenGoalEditorCommand.CanExecute(null));
    }

    [Fact]
    public void CreateAdjustmentSuggested_WithAction_OpensGoalEditor() {
        var goalEditorOpened = false;

        var viewModel = AdaptiveEnergyAssessmentViewModel.CreateAdjustmentSuggested(
            details: "Отклонение сохраняется несколько периодов.",
            recommendation: "Уменьшите норму на 100 ккал.",
            openGoalEditor: () => goalEditorOpened = true
        );

        Assert.True(viewModel.CanOpenGoalEditor);
        Assert.True(viewModel.OpenGoalEditorCommand.CanExecute(null));

        viewModel.OpenGoalEditorCommand.Execute(null);

        Assert.True(goalEditorOpened);
    }

    [Fact]
    public void CreateWithinTarget_DoesNotAllowOpeningGoalEditor() {
        var viewModel = AdaptiveEnergyAssessmentViewModel.CreateWithinTarget(
            "Фактический темп соответствует запланированному."
        );

        Assert.False(viewModel.CanOpenGoalEditor);
        Assert.False(viewModel.OpenGoalEditorCommand.CanExecute(null));
    }
}