using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Activities;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Activities;

public sealed class PlannedActivityCompletionServiceTests {
    [Fact]
    public void CreateCompletionDraft_CurrentPresetAndWeight_RecalculatesCalories() {
        var completionDate = new DateOnly(2026, 8, 18);
        var planStore = new InMemoryPlannedActivityStore();
        var presetStore = new InMemoryActivityPresetStore();
        var bodyStore = new InMemoryBodyMeasurementStore();

        var plan = new PlannedActivity(
            Id: Guid.NewGuid(),
            Date: completionDate,
            Name: "HEMA",
            Duration: TimeSpan.FromHours(1),
            PresetCode: "custom:hema",
            MetValue: 6m
        );

        planStore.Save(plan);
        presetStore.Save(new ActivityPreset("custom:hema", "HEMA", 7m));

        bodyStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: completionDate,
                WeightKg: 65m
            )
        );

        var service = CreateService(planStore, presetStore, bodyStore);
        var draft = service.CreateCompletionDraft(plan.Id, completionDate);

        Assert.NotNull(draft);
        Assert.Equal(390m, draft.BurnedCaloriesKcal);
        Assert.Equal(7m, draft.EnergyCalculation?.MetValue);
        Assert.Equal(65m, draft.EnergyCalculation?.WeightKg);
        Assert.NotEqual(plan.Id, draft.Id);
    }

    [Fact]
    public void CreateCompletionDraft_DeletedPreset_UsesStoredMet() {
        var completionDate = new DateOnly(2026, 8, 18);
        var planStore = new InMemoryPlannedActivityStore();
        var bodyStore = new InMemoryBodyMeasurementStore();

        var plan = new PlannedActivity(
            Id: Guid.NewGuid(),
            Date: completionDate,
            Name: "HEMA",
            Duration: TimeSpan.FromHours(1),
            PresetCode: "custom:deleted",
            MetValue: 6m
        );

        planStore.Save(plan);

        bodyStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: completionDate,
                WeightKg: 64m
            )
        );

        var service = CreateService(
            planStore,
            new InMemoryActivityPresetStore(),
            bodyStore
        );

        var draft = service.CreateCompletionDraft(plan.Id, completionDate);

        Assert.NotNull(draft);
        Assert.Equal(320m, draft.BurnedCaloriesKcal);
        Assert.Equal(6m, draft.EnergyCalculation?.MetValue);
    }

    [Fact]
    public void CreateCompletionDraft_ManualPlan_CopiesManualCalories() {
        var date = new DateOnly(2026, 8, 18);
        var store = new InMemoryPlannedActivityStore();

        var plan = new PlannedActivity(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Работа в саду",
            PlannedAt: new TimeOnly(11, 0),
            Duration: TimeSpan.FromMinutes(90),
            ManualBurnedCaloriesKcal: 250m,
            Note: "Обрезка"
        );

        store.Save(plan);

        var draft = CreateService(
            store,
            new InMemoryActivityPresetStore(),
            new InMemoryBodyMeasurementStore()
        ).CreateCompletionDraft(plan.Id, date);

        Assert.NotNull(draft);
        Assert.Equal(250m, draft.BurnedCaloriesKcal);
        Assert.Null(draft.EnergyCalculation);
        Assert.Equal(new TimeOnly(11, 0), draft.StartedAt);
        Assert.Equal("Обрезка", draft.Note);
    }

    private static PlannedActivityCompletionService CreateService(
        IPlannedActivityStore planStore,
        IActivityPresetStore presetStore,
        IBodyMeasurementStore bodyStore
    ) {
        var catalog = new ActivityPresetCatalogService(presetStore);
        var energy = new ActivityEnergySuggestionService(
            new BodyMeasurementHistoryService(bodyStore)
        );

        var draftFactory = new PlannedActivityCompletionDraftFactory(
            catalog,
            energy
        );

        return new PlannedActivityCompletionService(
            planStore,
            draftFactory
        );
    }
}
