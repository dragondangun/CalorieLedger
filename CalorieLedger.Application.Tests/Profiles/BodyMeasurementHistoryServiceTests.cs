using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class
    BodyMeasurementHistoryServiceTests {
    [Fact]
    public void Save_ValidEntry_SavesMeasurement() {
        var store =
            new InMemoryBodyMeasurementStore();

        var service =
            new BodyMeasurementHistoryService(
                store);

        var entry =
            CreateValidEntry(
                new DateOnly(
                    2026,
                    7,
                    17));

        var result =
            service.Save(
                entry,
                currentDate:
                    new DateOnly(
                        2026,
                        7,
                        17));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);

        Assert.Equal(
            entry,
            Assert.Single(
                service.GetAll()));
    }

    [Fact]
    public void Save_FutureDate_DoesNotSaveMeasurement() {
        var store =
            new InMemoryBodyMeasurementStore();

        var service =
            new BodyMeasurementHistoryService(
                store);

        var entry =
            CreateValidEntry(
                new DateOnly(
                    2026,
                    7,
                    18));

        var result =
            service.Save(
                entry,
                currentDate:
                    new DateOnly(
                        2026,
                        7,
                        17));

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError
                .FutureDate,
            result.Errors);

        Assert.Empty(
            service.GetAll());
    }

    [Fact]
    public void Save_EmptyId_DoesNotSaveMeasurement() {
        var store =
            new InMemoryBodyMeasurementStore();

        var service =
            new BodyMeasurementHistoryService(
                store);

        var entry =
            CreateValidEntry(
                new DateOnly(
                    2026,
                    7,
                    17)) with
            {
                Id = Guid.Empty
            };

        var result =
            service.Save(
                entry,
                currentDate:
                    new DateOnly(
                        2026,
                        7,
                        17));

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError
                .MissingId,
            result.Errors);

        Assert.Empty(
            service.GetAll());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Save_InvalidWeight_DoesNotSaveMeasurement(
        int weightKg) {
        var store =
            new InMemoryBodyMeasurementStore();

        var service =
            new BodyMeasurementHistoryService(
                store);

        var entry =
            CreateValidEntry(
                new DateOnly(
                    2026,
                    7,
                    17)) with
            {
                WeightKg = weightKg
            };

        var result =
            service.Save(
                entry,
                currentDate:
                    new DateOnly(
                        2026,
                        7,
                        17));

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError
                .InvalidWeight,
            result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-5)]
    public void Save_InvalidBodyFatPercent_ReturnsError(
        int bodyFatPercent) {
        var store =
            new InMemoryBodyMeasurementStore();

        var service =
            new BodyMeasurementHistoryService(
                store);

        var entry =
            CreateValidEntry(
                new DateOnly(
                    2026,
                    7,
                    17)) with
            {
                BodyFatPercent =
                    bodyFatPercent
            };

        var result =
            service.Save(
                entry,
                currentDate:
                    new DateOnly(
                        2026,
                        7,
                        17));

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError
                .InvalidBodyFatPercent,
            result.Errors);
    }

    [Fact]
    public void Save_SeveralInvalidValues_ReturnsAllErrors() {
        var store =
            new InMemoryBodyMeasurementStore();

        var service =
            new BodyMeasurementHistoryService(
                store);

        var entry =
            new BodyMeasurementEntry(
                Id: Guid.Empty,
                Date:
                    new DateOnly(
                        2026,
                        7,
                        18),
                WeightKg: 0m,
                BodyFatPercent: 100m,
                BoneMassKg: 0m,
                MuscleMassKg: -1m,
                MusclePercent: 0m);

        var result =
            service.Save(
                entry,
                currentDate:
                    new DateOnly(
                        2026,
                        7,
                        17));

        Assert.False(result.IsSuccess);

        Assert.Equal(
            7,
            result.Errors.Count);

        Assert.Empty(
            service.GetAll());
    }

    [Fact]
    public void Delete_ExistingMeasurement_RemovesIt() {
        var store =
            new InMemoryBodyMeasurementStore();

        var service =
            new BodyMeasurementHistoryService(
                store);

        var entry =
            CreateValidEntry(
                new DateOnly(
                    2026,
                    7,
                    17));

        service.Save(
            entry,
            currentDate:
                new DateOnly(
                    2026,
                    7,
                    17));

        var deleted =
            service.Delete(entry.Id);

        Assert.True(deleted);
        Assert.Empty(service.GetAll());
    }

    private static BodyMeasurementEntry
        CreateValidEntry(
        DateOnly date) {
        return new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: date,
            WeightKg: 80m,
            BodyFatPercent: 20m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35m,
            MusclePercent: 43.75m);
    }

    [Fact]
    public void Save_MuscleMassOnly_CalculatesMusclePercent() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(
            store);

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m,
            MuscleMassKg: 35m,
            MusclePercent: null);

        var result = service.Save(
            entry,
            currentDate);

        Assert.True(result.IsSuccess);

        var savedEntry = Assert.Single(service.GetAll());

        Assert.Equal(
            43.75m,
            savedEntry.MusclePercent);
    }

    [Fact]
    public void Save_MusclePercentOnly_CalculatesMuscleMass() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(
            store);

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m,
            MuscleMassKg: null,
            MusclePercent: 43.75m);

        var result = service.Save(
            entry,
            currentDate);

        Assert.True(result.IsSuccess);

        var savedEntry = Assert.Single(
            service.GetAll());

        Assert.Equal(
            35m,
            savedEntry.MuscleMassKg);
    }

    [Fact]
    public void Save_InconsistentMuscleValues_DoesNotSave() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m,
            MuscleMassKg: 35m,
            MusclePercent: 40m);

        var result = service.Save(
            entry,
            currentDate);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError.InconsistentMuscleValues,
            result.Errors);

        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void Save_MuscleMassGreaterThanWeight_DoesNotSave() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m,
            MuscleMassKg: 81m);

        var result = service.Save(
            entry,
            currentDate);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError.InvalidMuscleMass,
            result.Errors);

        Assert.Empty(service.GetAll());
    }
} 