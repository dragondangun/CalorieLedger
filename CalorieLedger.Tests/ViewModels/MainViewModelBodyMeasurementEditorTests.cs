using CalorieLedger.ViewModels;

namespace CalorieLedger.Tests.ViewModels;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

public sealed class
    MainViewModelBodyMeasurementEditorTests {
    [Fact]
    public void AddBodyMeasurementCommand_OpensEditor() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

        viewModel.AddBodyMeasurementCommand.Execute(null);

        Assert.True(viewModel.IsBodyMeasurementEditorOpen);

        Assert.False(viewModel.IsTodayDashboardVisible);

        Assert.NotNull(viewModel.BodyMeasurementEditor);
    }

    [Fact]
    public void CancelMeasurementEditing_ReturnsToDashboard() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

        viewModel.AddBodyMeasurementCommand.Execute(null);

        Assert.NotNull(viewModel.BodyMeasurementEditor);

        viewModel.BodyMeasurementEditor.CancelCommand.Execute(null);

        Assert.False(viewModel.IsBodyMeasurementEditorOpen);

        Assert.True(viewModel.IsTodayDashboardVisible);

        Assert.Null(viewModel.BodyMeasurementEditor);
    }

    [Fact]
    public void SaveMeasurement_AddsItToHistory() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

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
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

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
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

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

    [Fact]
    public void SaveMeasurement_RefreshesTodayDashboard() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

        var previousToday = viewModel.Today;

        viewModel.AddBodyMeasurementCommand.Execute(null);

        Assert.NotNull(viewModel.BodyMeasurementEditor);

        viewModel.BodyMeasurementEditor.WeightKg = 70m;

        viewModel.BodyMeasurementEditor.SaveCommand.Execute(null);

        Assert.NotSame(
            previousToday,
            viewModel.Today);

        Assert.Contains(
            "Измерение сохранено",
            viewModel.Today.GoalActionSelectionSummary);
    }

    [Fact]
    public void DeleteMeasurement_RefreshesTodayDashboard() {
        var viewModel = new MainViewModel(new InMemoryBodyMeasurementStore());

        viewModel.AddBodyMeasurementCommand.Execute(null);

        Assert.NotNull(viewModel.BodyMeasurementEditor);

        viewModel.BodyMeasurementEditor.WeightKg = 70m;

        viewModel.BodyMeasurementEditor.SaveCommand.Execute(null);

        var todayAfterSave = viewModel.Today;

        var measurement = Assert.Single(viewModel.BodyMeasurements);

        measurement.DeleteCommand.Execute(null);

        Assert.NotSame(todayAfterSave, viewModel.Today);

        Assert.Contains(
            "Измерение удалено",
            viewModel.Today.GoalActionSelectionSummary);
    }

    [Fact]
    public void Constructor_LoadsExistingMeasurementsFromStore() {
        var store = new InMemoryBodyMeasurementStore();

        var historyService = new BodyMeasurementHistoryService(store);

        var currentDate = new DateOnly(
            2026,
            7,
            20);

        historyService.Save(new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                WeightKg: 80m),
            currentDate);

        var viewModel = new MainViewModel(store);

        var measurement = Assert.Single(viewModel.BodyMeasurements);

        Assert.Equal(
            "80,0 кг",
            measurement.WeightSummary);
    }

    [Fact]
    public void Constructor_WithTrendHistory_CreatesTrendCards() {
        var store = new InMemoryBodyMeasurementStore();

        var currentDate = DateOnly.FromDateTime(DateTime.Today);

        for(var dayOffset = 21; dayOffset >= 0; dayOffset--) {
            var elapsedDays = 21 - dayOffset;

            decimal? bodyFatPercent = dayOffset % 3 == 0
                ? 25m - elapsedDays * 0.02m
                : null;

            store.Save(
                new BodyMeasurementEntry(
                    Id: Guid.NewGuid(),
                    Date: currentDate.AddDays(-dayOffset),
                    WeightKg: 80m - elapsedDays * 0.05m,
                    BodyFatPercent: bodyFatPercent));
        }

        var viewModel = new MainViewModel(store);

        Assert.True(viewModel.BodyTrends.WeightTrend.IsAvailable);

        Assert.True(viewModel.BodyTrends.BodyFatTrend.IsAvailable);

        Assert.True(viewModel.BodyTrends.HasAnyAvailableTrend);
    }
}