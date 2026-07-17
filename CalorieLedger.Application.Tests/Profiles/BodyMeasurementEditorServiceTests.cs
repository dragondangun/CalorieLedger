using CalorieLedger.Application.Profiles;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class BodyMeasurementEditorServiceTests {
    [Fact]
    public void CreateNew_ReturnsEmptyDraftForCurrentDate() {
        var service =
            CreateService();

        var currentDate =
            new DateOnly(
                2026,
                7,
                18);

        var draft =
            service.CreateNew(currentDate);

        Assert.NotEqual(
            Guid.Empty,
            draft.Id);

        Assert.Equal(
            currentDate,
            draft.Date);

        Assert.Null(draft.WeightKg);
        Assert.Null(draft.BodyFatPercent);
        Assert.Null(draft.BoneMassKg);
        Assert.Null(draft.MuscleMassKg);
        Assert.Null(draft.MusclePercent);
    }

    [Fact]
    public void Save_ValidDraft_SavesMeasurement() {
        var store =
            new InMemoryBodyMeasurementStore();

        var historyService =
            new BodyMeasurementHistoryService(
                store);

        var editorService =
            new BodyMeasurementEditorService(
                historyService);

        var currentDate =
            new DateOnly(
                2026,
                7,
                18);

        var draft =
            editorService
                .CreateNew(currentDate) with
            {
                WeightKg = 80m,
                BodyFatPercent = 20m
            };

        var result =
            editorService.Save(
                draft,
                currentDate);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);

        var savedEntry =
            Assert.Single(
                historyService.GetAll());

        Assert.Equal(
            draft.Id,
            savedEntry.Id);

        Assert.Equal(
            80m,
            savedEntry.WeightKg);

        Assert.Equal(
            20m,
            savedEntry.BodyFatPercent);
    }

    [Fact]
    public void Save_MissingWeight_ReturnsValidationError() {
        var store =
            new InMemoryBodyMeasurementStore();

        var historyService =
            new BodyMeasurementHistoryService(
                store);

        var editorService =
            new BodyMeasurementEditorService(
                historyService);

        var currentDate =
            new DateOnly(
                2026,
                7,
                18);

        var draft =
            editorService.CreateNew(
                currentDate);

        var result =
            editorService.Save(
                draft,
                currentDate);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError
                .InvalidWeight,
            result.Errors);

        Assert.Empty(
            historyService.GetAll());
    }

    [Fact]
    public void Load_ExistingMeasurement_ReturnsDraft() {
        var store =
            new InMemoryBodyMeasurementStore();

        var historyService =
            new BodyMeasurementHistoryService(
                store);

        var editorService =
            new BodyMeasurementEditorService(
                historyService);

        var currentDate =
            new DateOnly(
                2026,
                7,
                18);

        var originalDraft =
            editorService
                .CreateNew(currentDate) with
            {
                WeightKg = 80m,
                BodyFatPercent = 20m
            };

        editorService.Save(
            originalDraft,
            currentDate);

        var loadedDraft =
            editorService.Load(
                originalDraft.Id);

        Assert.NotNull(loadedDraft);

        Assert.Equal(
            originalDraft,
            loadedDraft);
    }

    [Fact]
    public void Load_UnknownId_ReturnsNull() {
        var service =
            CreateService();

        var result =
            service.Load(
                Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void Save_ExistingDraft_UpdatesInsteadOfAdding() {
        var store =
            new InMemoryBodyMeasurementStore();

        var historyService =
            new BodyMeasurementHistoryService(
                store);

        var editorService =
            new BodyMeasurementEditorService(
                historyService);

        var currentDate =
            new DateOnly(
                2026,
                7,
                18);

        var draft =
            editorService
                .CreateNew(currentDate) with
            {
                WeightKg = 80m
            };

        editorService.Save(
            draft,
            currentDate);

        var updatedDraft =
            draft with
            {
                WeightKg = 79.5m
            };

        editorService.Save(
            updatedDraft,
            currentDate);

        var savedEntry =
            Assert.Single(
                historyService.GetAll());

        Assert.Equal(
            79.5m,
            savedEntry.WeightKg);
    }

    [Fact]
    public void Delete_ExistingMeasurement_RemovesIt() {
        var store =
            new InMemoryBodyMeasurementStore();

        var historyService =
            new BodyMeasurementHistoryService(
                store);

        var editorService =
            new BodyMeasurementEditorService(
                historyService);

        var currentDate =
            new DateOnly(
                2026,
                7,
                18);

        var draft =
            editorService
                .CreateNew(currentDate) with
            {
                WeightKg = 80m
            };

        editorService.Save(
            draft,
            currentDate);

        var deleted =
            editorService.Delete(
                draft.Id);

        Assert.True(deleted);
        Assert.Empty(historyService.GetAll());
    }

    private static BodyMeasurementEditorService
        CreateService() {
        var store =
            new InMemoryBodyMeasurementStore();

        var historyService =
            new BodyMeasurementHistoryService(
                store);

        return new BodyMeasurementEditorService(
            historyService);
    }
}