using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Activities;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Activities;

public sealed class ActivityRepeatServiceTests {
    [Fact]
    public void CreateDraft_ManualActivity_CopiesReusableFieldsAndCreatesNewEntry() {
        var sourceDate = new DateOnly(2026, 8, 10);
        var targetDate = new DateOnly(2026, 8, 18);
        var sourceId = Guid.NewGuid();

        var activityStore = new InMemoryActivityStore();

        activityStore.Save(
            new ActivityEntry(
                Id: sourceId,
                Date: sourceDate,
                Name: "Прогулка",
                BurnedCaloriesKcal: 180m,
                StartedAt: new TimeOnly(18, 30),
                Duration: TimeSpan.FromMinutes(45),
                Note: "Старая заметка"
            )
        );

        var service = CreateService(activityStore);
        var draft = service.CreateDraft(sourceId, targetDate);

        Assert.NotNull(draft);
        Assert.NotEqual(sourceId, draft.Id);
        Assert.Equal(targetDate, draft.Date);
        Assert.Equal("Прогулка", draft.Name);
        Assert.Equal(180m, draft.BurnedCaloriesKcal);
        Assert.Equal(TimeSpan.FromMinutes(45), draft.Duration);
        Assert.Null(draft.StartedAt);
        Assert.Null(draft.Note);
        Assert.Null(draft.EnergyCalculation);
    }

    [Fact]
    public void CreateDraft_EstimatedActivity_RecalculatesUsingTargetWeightAndCurrentMet() {
        var sourceDate = new DateOnly(2026, 8, 10);
        var targetDate = new DateOnly(2026, 8, 18);
        var sourceId = Guid.NewGuid();

        var activityStore = new InMemoryActivityStore();
        var presetStore = new InMemoryActivityPresetStore();
        var bodyStore = new InMemoryBodyMeasurementStore();

        activityStore.Save(
            new ActivityEntry(
                Id: sourceId,
                Date: sourceDate,
                Name: "HEMA",
                BurnedCaloriesKcal: 300m,
                Duration: TimeSpan.FromHours(1),
                EnergyCalculation: new ActivityEnergyCalculation(
                    PresetCode: "custom:hema",
                    MetValue: 6m,
                    WeightKg: 60m,
                    DurationMinutes: 60m
                )
            )
        );

        presetStore.Save(
            new ActivityPreset(
                Code: "custom:hema",
                Name: "HEMA",
                MetValue: 7m
            )
        );

        bodyStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: targetDate,
                WeightKg: 65m
            )
        );

        var service = CreateService(
            activityStore,
            presetStore,
            bodyStore
        );

        var draft = service.CreateDraft(sourceId, targetDate);

        Assert.NotNull(draft);
        Assert.Equal(390m, draft.BurnedCaloriesKcal);
        Assert.NotNull(draft.EnergyCalculation);
        Assert.Equal(7m, draft.EnergyCalculation.MetValue);
        Assert.Equal(65m, draft.EnergyCalculation.WeightKg);
        Assert.Equal(60m, draft.EnergyCalculation.DurationMinutes);
    }

    [Fact]
    public void CreateDraft_DeletedPreset_UsesHistoricalMetForRecalculation() {
        var sourceDate = new DateOnly(2026, 8, 10);
        var targetDate = new DateOnly(2026, 8, 18);
        var sourceId = Guid.NewGuid();

        var activityStore = new InMemoryActivityStore();
        var bodyStore = new InMemoryBodyMeasurementStore();

        activityStore.Save(
            new ActivityEntry(
                Id: sourceId,
                Date: sourceDate,
                Name: "HEMA",
                BurnedCaloriesKcal: 300m,
                Duration: TimeSpan.FromHours(1),
                EnergyCalculation: new ActivityEnergyCalculation(
                    PresetCode: "custom:deleted",
                    MetValue: 6m,
                    WeightKg: 60m,
                    DurationMinutes: 60m
                )
            )
        );

        bodyStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: targetDate,
                WeightKg: 64m
            )
        );

        var service = CreateService(
            activityStore,
            bodyStore: bodyStore
        );

        var draft = service.CreateDraft(sourceId, targetDate);

        Assert.NotNull(draft);
        Assert.Equal(320m, draft.BurnedCaloriesKcal);
        Assert.NotNull(draft.EnergyCalculation);
        Assert.Equal("custom:deleted", draft.EnergyCalculation.PresetCode);
        Assert.Equal(6m, draft.EnergyCalculation.MetValue);
        Assert.Equal(64m, draft.EnergyCalculation.WeightKg);
    }

    [Fact]
    public void CreateDraft_EstimatedActivityWithoutTargetWeight_FallsBackToManualCalories() {
        var sourceId = Guid.NewGuid();
        var activityStore = new InMemoryActivityStore();

        activityStore.Save(
            new ActivityEntry(
                Id: sourceId,
                Date: new DateOnly(2026, 8, 10),
                Name: "Фехтование",
                BurnedCaloriesKcal: 300m,
                Duration: TimeSpan.FromHours(1),
                EnergyCalculation: new ActivityEnergyCalculation(
                    PresetCode: "15200",
                    MetValue: 6m,
                    WeightKg: 60m,
                    DurationMinutes: 60m
                )
            )
        );

        var service = CreateService(activityStore);

        var draft = service.CreateDraft(
            sourceId,
            new DateOnly(2026, 8, 18)
        );

        Assert.NotNull(draft);
        Assert.Equal(300m, draft.BurnedCaloriesKcal);
        Assert.Null(draft.EnergyCalculation);
    }

    private static ActivityRepeatService CreateService(
        IActivityStore activityStore,
        IActivityPresetStore? presetStore = null,
        IBodyMeasurementStore? bodyStore = null
    ) {
        var catalogService = new ActivityPresetCatalogService(
            presetStore ?? new InMemoryActivityPresetStore()
        );

        var energyService = new ActivityEnergySuggestionService(
            new BodyMeasurementHistoryService(
                bodyStore ?? new InMemoryBodyMeasurementStore()
            )
        );

        return new ActivityRepeatService(
            activityStore,
            catalogService,
            energyService
        );
    }
}
