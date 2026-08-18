using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Activities;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Activities;

namespace CalorieLedger.Tests.ViewModels.Activities;

public sealed class ActivityEditorRecentActivityTests {
    [Fact]
    public void ApplyRecentActivity_FillsNewEditorFromRepeatDraft() {
        var targetDate = new DateOnly(2026, 8, 18);
        var activityStore = new InMemoryActivityStore();
        var bodyStore = new InMemoryBodyMeasurementStore();
        var presetStore = new InMemoryActivityPresetStore();

        var source = new ActivityEntry(
            Id: Guid.NewGuid(),
            Date: targetDate.AddDays(-7),
            Name: "HEMA",
            BurnedCaloriesKcal: 300m,
            StartedAt: new TimeOnly(19, 0),
            Duration: TimeSpan.FromHours(1),
            Note: "Старая заметка",
            EnergyCalculation: new ActivityEnergyCalculation(
                PresetCode: "custom:hema",
                MetValue: 6m,
                WeightKg: 60m,
                DurationMinutes: 60m
            )
        );

        activityStore.Save(source);
        presetStore.Save(new ActivityPreset("custom:hema", "HEMA", 7m));

        bodyStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: targetDate,
                WeightKg: 65m
            )
        );

        var editorService = new ActivityEditorService(activityStore);
        var catalogService = new ActivityPresetCatalogService(presetStore);
        var energyService = new ActivityEnergySuggestionService(
            new BodyMeasurementHistoryService(bodyStore)
        );
        var repeatService = new ActivityRepeatService(
            activityStore,
            catalogService,
            energyService
        );

        var viewModel = new ActivityEditorViewModel(
            editorService,
            editorService.CreateNew(targetDate),
            targetDate,
            true,
            () => { },
            () => { },
            catalogService,
            energyService,
            new RecentActivityService(activityStore),
            repeatService
        );

        var recent = Assert.Single(viewModel.RecentActivities);
        recent.ApplyCommand.Execute(null);

        Assert.Equal("HEMA", viewModel.Name);
        Assert.Equal(60m, viewModel.DurationMinutes);
        Assert.Equal(390m, viewModel.BurnedCaloriesKcal);
        Assert.Null(viewModel.StartedAtTime);
        Assert.Null(viewModel.Note);
        Assert.Equal("custom:hema", viewModel.SelectedPreset?.Code);
    }
}
