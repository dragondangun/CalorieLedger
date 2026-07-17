using CalorieLedger.ViewModels;

namespace CalorieLedger.Tests.ViewModels;

public sealed class
    MainViewModelBodyMeasurementEditorTests {
    [Fact]
    public void AddBodyMeasurementCommand_OpensEditor() {
        var viewModel =
            new MainViewModel();

        viewModel
            .AddBodyMeasurementCommand
            .Execute(null);

        Assert.True(
            viewModel
                .IsBodyMeasurementEditorOpen);

        Assert.False(
            viewModel
                .IsTodayDashboardVisible);

        Assert.NotNull(
            viewModel.BodyMeasurementEditor);
    }

    [Fact]
    public void CancelMeasurementEditing_ReturnsToDashboard() {
        var viewModel = new MainViewModel();

        viewModel
            .AddBodyMeasurementCommand
            .Execute(null);

        Assert.NotNull(viewModel.BodyMeasurementEditor);

        viewModel
            .BodyMeasurementEditor
            .CancelCommand
            .Execute(null);

        Assert.False(
            viewModel
                .IsBodyMeasurementEditorOpen);

        Assert.True(
            viewModel
                .IsTodayDashboardVisible);

        Assert.Null(viewModel.BodyMeasurementEditor);
    }
}