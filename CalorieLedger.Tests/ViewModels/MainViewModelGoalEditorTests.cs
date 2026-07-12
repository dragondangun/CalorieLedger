using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelGoalEditorTests {
    [Fact]
    public void SetNewGoalAction_OpensGoalEditor() {
        var viewModel = new MainViewModel();

        var action =
            viewModel.Today.GoalActions.Single(
                x => x.Action == GoalNextAction.SetNewGoal);

        action.SelectCommand.Execute(null);

        Assert.True(viewModel.IsGoalEditorOpen);
        Assert.False(viewModel.IsTodayDashboardVisible);
        Assert.NotNull(viewModel.GoalEditor);
    }

    [Fact]
    public void CancelGoalEditing_ReturnsToTodayDashboard() {
        var viewModel = new MainViewModel();

        var action =
            viewModel.Today.GoalActions.Single(
                x => x.Action == GoalNextAction.SetNewGoal);

        action.SelectCommand.Execute(null);

        Assert.NotNull(viewModel.GoalEditor);

        viewModel.GoalEditor.CancelCommand.Execute(null);

        Assert.False(viewModel.IsGoalEditorOpen);
        Assert.True(viewModel.IsTodayDashboardVisible);
        Assert.Null(viewModel.GoalEditor);
    }

    [Fact]
    public void SaveValidGoal_ClosesEditorAndRefreshesDashboard() {
        var viewModel = new MainViewModel();

        var action =
            viewModel.Today.GoalActions.Single(
                x => x.Action == GoalNextAction.SetNewGoal);

        action.SelectCommand.Execute(null);

        var editor = Assert.IsType<
            CalorieLedger.ViewModels.Profile
                .NutritionGoalEditorViewModel>(
            viewModel.GoalEditor);

        editor.GoalType =
            WeightGoalType.LoseWeight;

        editor.TargetWeightKg = 75m;
        editor.TargetBodyFatPercent = 15m;

        editor.DesiredWeightChangeKgPerWeek = null;
        editor.EnergyBalancePercent = -10m;

        editor.SaveCommand.Execute(null);

        Assert.False(viewModel.IsGoalEditorOpen);
        Assert.True(viewModel.IsTodayDashboardVisible);
        Assert.Null(viewModel.GoalEditor);

        Assert.Contains(
            "Цель сохранена",
            viewModel.Today.GoalActionSelectionSummary);
    }

    [Fact]
    public void SetNewGoalAction_RaisesVisibilityPropertyChangedEvents() {
        var viewModel = new MainViewModel();

        var changedProperties = new List<string>();

        viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName is not null) {
                changedProperties.Add(args.PropertyName);
            }
        };

        var action =
        viewModel.Today.GoalActions.Single(
            x => x.Action == GoalNextAction.SetNewGoal);

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
}