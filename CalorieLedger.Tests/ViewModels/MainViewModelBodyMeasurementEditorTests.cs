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

    [Fact]
    public void SaveMeasurement_AddsItToHistory() {
        var viewModel = new MainViewModel();

        viewModel.AddBodyMeasurementCommand.Execute(null);

        Assert.NotNull(viewModel.BodyMeasurementEditor);

        viewModel.BodyMeasurementEditor.WeightKg = 80m;

        viewModel.BodyMeasurementEditor.SaveCommand.Execute(null);

        var measurement = Assert.Single(viewModel.BodyMeasurements);

        Assert.Equal(
            "80,0 кг",
            measurement.WeightSummary);

        Assert.True(viewModel.HasBodyMeasurements);

        Assert.False(viewModel.HasNoBodyMeasurements);
    }

    [Fact]
    public void EditMeasurement_UpdatesExistingHistoryItem() {
        var viewModel = new MainViewModel();

        viewModel.AddBodyMeasurementCommand.Execute(null);

        Assert.NotNull(viewModel.BodyMeasurementEditor);

        viewModel.BodyMeasurementEditor.WeightKg = 80m;

        viewModel.BodyMeasurementEditor.SaveCommand.Execute(null);

        var measurement = Assert.Single(viewModel.BodyMeasurements);

        measurement.EditCommand.Execute(null);

        Assert.NotNull(viewModel.BodyMeasurementEditor);

        Assert.Equal(
            80m,
            viewModel.BodyMeasurementEditor.WeightKg);

        viewModel.BodyMeasurementEditor.WeightKg = 79.5m;

        viewModel.BodyMeasurementEditor.SaveCommand.Execute(null);

        var updatedMeasurement = Assert.Single(viewModel.BodyMeasurements);

        Assert.Equal(
            "79,5 кг",
            updatedMeasurement.WeightSummary);
    }

    [Fact]
    public void DeleteMeasurement_RemovesHistoryItem() {
        var viewModel = new MainViewModel();

        viewModel.AddBodyMeasurementCommand.Execute(null);

        Assert.NotNull(viewModel.BodyMeasurementEditor);

        viewModel.BodyMeasurementEditor.WeightKg = 80m;

        viewModel.BodyMeasurementEditor.SaveCommand.Execute(null);

        var measurement = Assert.Single(viewModel.BodyMeasurements);

        measurement.DeleteCommand.Execute(null);

        Assert.Empty(viewModel.BodyMeasurements);

        Assert.False(viewModel.HasBodyMeasurements);

        Assert.True(viewModel.HasNoBodyMeasurements);
    }
}