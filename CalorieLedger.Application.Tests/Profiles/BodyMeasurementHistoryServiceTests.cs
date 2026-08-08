using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class BodyMeasurementHistoryServiceTests {
    [Fact]
    public void Save_ValidEntry_SavesMeasurement() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var entry = CreateValidEntry(new DateOnly(
            2026,
            7,
            17)
        );

        var result = service.Save(
            entry,
            currentDate: new DateOnly(
                2026,
                7,
                17)
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);

        Assert.Equal(
            entry,
            Assert.Single(service.GetAll())
        );
    }

    [Fact]
    public void Save_FutureDate_DoesNotSaveMeasurement() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var entry = CreateValidEntry(new DateOnly(
            2026,
            7,
            18)
        );

        var result = service.Save(
            entry,
            currentDate: new DateOnly(
                2026,
                7,
                17)
        );

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError.FutureDate,
            result.Errors
        );

        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void Save_EmptyId_DoesNotSaveMeasurement() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var entry = CreateValidEntry(
            new DateOnly(
                2026,
                7,
                17)
        ) with {
            Id = Guid.Empty
        };

        var result = service.Save(
            entry,
            currentDate: new DateOnly(
                2026,
                7,
                17)
        );

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError.MissingId,
            result.Errors
        );

        Assert.Empty(service.GetAll());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Save_InvalidWeight_DoesNotSaveMeasurement(int weightKg) {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var entry = CreateValidEntry(
            new DateOnly(
                2026,
                7,
                17))
        with {
            WeightKg = weightKg
        };

        var result = service.Save(
            entry,
            currentDate: new DateOnly(
                2026,
                7,
                17)
        );

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError.InvalidWeight,
            result.Errors
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-5)]
    public void Save_InvalidBodyFatPercent_ReturnsError(int bodyFatPercent) {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var entry = CreateValidEntry(
            new DateOnly(
                2026,
                7,
                17)
        ) with {
            BodyFatPercent = bodyFatPercent
        };

        var result = service.Save(
            entry,
            currentDate: new DateOnly(
                2026,
                7,
                17)
        );

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError.InvalidBodyFatPercent,
            result.Errors
        );
    }

    [Fact]
    public void Save_SeveralInvalidValues_ReturnsAllErrors() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var entry = new BodyMeasurementEntry(
            Id: Guid.Empty,
            Date: new DateOnly(
                2026,
                7,
                18),
            WeightKg: 0m,
            BodyFatPercent: 100m,
            BoneMassKg: 0m,
            MuscleMassKg: -1m,
            MusclePercent: 0m);

        var result = service.Save(
            entry,
            currentDate: new DateOnly(
                2026,
                7,
                17)
        );

        Assert.False(result.IsSuccess);

        Assert.Equal(
            7,
            result.Errors.Count);

        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void Delete_ExistingMeasurement_RemovesIt() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var entry = CreateValidEntry(new DateOnly(
            2026,
            7,
            17)
        );

        service.Save(
            entry,
            currentDate: new DateOnly(
                2026,
                7,
                17)
        );

        var deleted = service.Delete(entry.Id);

        Assert.True(deleted);
        Assert.Empty(service.GetAll());
    }

    private static BodyMeasurementEntry CreateValidEntry(DateOnly date) {
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

        var service = new BodyMeasurementHistoryService(store);

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

    [Fact]
    public void GetLatest_EmptyHistory_ReturnsNull() {
        var service = new BodyMeasurementHistoryService(
            new InMemoryBodyMeasurementStore()
        );

        var result = service.GetLatestByDate();

        Assert.Null(result);
    }

    [Fact]
    public void GetLatest_ReturnsMeasurementWithLatestDate() {
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var currentDate = new DateOnly(
            2026,
            7,
            26
        );

        var latestMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 79m
        );

        service.Save(
            latestMeasurement,
            currentDate
        );

        service.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate.AddDays(-5),
                WeightKg: 80m
            ),
            currentDate
        );

        var result = service.GetLatestByDate();

        Assert.Equal(
            latestMeasurement,
            result
        );
    }

    [Fact]
    public void GetByDate_ExistingMeasurement_ReturnsMeasurement() {
        var store = new InMemoryBodyMeasurementStore();
        var service = new BodyMeasurementHistoryService(store);
        var currentDate = new DateOnly(2026, 7, 26);

        var measurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m
        );

        service.Save(
            measurement,
            currentDate
        );

        var result = service.GetByDate(currentDate);

        Assert.Equal(
            measurement,
            result
        );
    }

    [Fact]
    public void GetByDate_MissingDate_ReturnsNull() {
        var service = new BodyMeasurementHistoryService(
            new InMemoryBodyMeasurementStore()
        );

        var result = service.GetByDate(
            new DateOnly(2026, 7, 26)
        );

        Assert.Null(result);
    }

    [Fact]
    public void Save_SecondMeasurementForSameDate_Fails() {
        var store = new InMemoryBodyMeasurementStore();
        var service = new BodyMeasurementHistoryService(store);
        var currentDate = new DateOnly(2026, 7, 26);

        service.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                WeightKg: 80m
            ),
            currentDate
        );

        var result = service.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                WeightKg: 79.5m
            ),
            currentDate
        );

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError.DuplicateDate,
            result.Errors
        );

        Assert.Single(store.GetAll());
    }

    [Fact]
    public void Save_ExistingMeasurementOnSameDate_UpdatesIt() {
        var store = new InMemoryBodyMeasurementStore();
        var service = new BodyMeasurementHistoryService(store);
        var currentDate = new DateOnly(2026, 7, 26);
        var id = Guid.NewGuid();

        service.Save(
            new BodyMeasurementEntry(
                Id: id,
                Date: currentDate,
                WeightKg: 80m
            ),
            currentDate
        );

        var result = service.Save(
            new BodyMeasurementEntry(
                Id: id,
                Date: currentDate,
                WeightKg: 79.5m
            ),
            currentDate
        );

        Assert.True(result.IsSuccess);

        var savedMeasurement = Assert.Single(store.GetAll());

        Assert.Equal(
            79.5m,
            savedMeasurement.WeightKg
        );
    }

    [Fact]
    public void Save_MovingMeasurementToOccupiedDate_Fails() {
        var store = new InMemoryBodyMeasurementStore();
        var service = new BodyMeasurementHistoryService(store);
        var currentDate = new DateOnly(2026, 7, 26);

        var firstMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(-1),
            WeightKg: 80m
        );

        var secondMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 79.5m
        );

        service.Save(
            firstMeasurement,
            currentDate
        );

        service.Save(
            secondMeasurement,
            currentDate
        );

        var movedMeasurement = firstMeasurement with {
            Date = currentDate,
        };

        var result = service.Save(
            movedMeasurement,
            currentDate
        );

        Assert.False(result.IsSuccess);

        Assert.Contains(
            BodyMeasurementValidationError.DuplicateDate,
            result.Errors
        );

        Assert.Equal(
            2,
            store.GetAll().Count
        );
    }

    [Fact]
    public void Save_FutureDate_ReturnsValidationError() {
        var currentDate = new DateOnly(2026, 8, 8);
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(
            store
        );

        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(1),
            WeightKg: 80m
        );

        var result = service.Save(entry, currentDate);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            BodyMeasurementValidationError.FutureDate,
            result.Errors
        );

        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Save_CurrentDate_AllowsMeasurement() {
        var currentDate = new DateOnly(2026, 8, 8);
        var store = new InMemoryBodyMeasurementStore();

        var service = new BodyMeasurementHistoryService(store);

        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m
        );

        var result = service.Save(entry, currentDate);

        Assert.True(result.IsSuccess);
        Assert.Single(store.GetAll());
    }

    [Fact]
    public void GetLatestOnOrBefore_FutureMeasurement_IgnoresFutureMeasurement() {
        var currentDate = new DateOnly(2026, 8, 8);
        var store = new InMemoryBodyMeasurementStore();

        store.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate.AddDays(-1),
                WeightKg: 80m
            )
        );

        store.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate.AddDays(1),
                WeightKg: 81m
            )
        );

        var service = new BodyMeasurementHistoryService(store);

        var latestMeasurement = service.GetLatestOnOrBefore(currentDate);

        Assert.NotNull(latestMeasurement);
        Assert.Equal(
            currentDate.AddDays(-1),
            latestMeasurement.Date
        );
    }

    [Fact]
    public void GetLatestOnOrBefore_OnlyFutureMeasurements_ReturnsNull() {
        var currentDate = new DateOnly(2026, 8, 8);
        var store = new InMemoryBodyMeasurementStore();

        store.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate.AddDays(1),
                WeightKg: 80m
            )
        );

        var service = new BodyMeasurementHistoryService(store);

        var latestMeasurement = service.GetLatestOnOrBefore(currentDate);

        Assert.Null(latestMeasurement);
    }

    [Fact]
    public void GetLatestOnOrBefore_CurrentDateMeasurement_IncludesMeasurement() {
        var currentDate = new DateOnly(2026, 8, 8);
        var store = new InMemoryBodyMeasurementStore();

        store.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate.AddDays(-1),
                WeightKg: 79m
            )
        );

        store.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                WeightKg: 80m
            )
        );

        var service = new BodyMeasurementHistoryService(store);

        var latestMeasurement = service.GetLatestOnOrBefore(currentDate);

        Assert.NotNull(latestMeasurement);
        Assert.Equal(
            currentDate,
            latestMeasurement.Date
        );
    }

    [Fact]
    public void LatestLookups_FutureMeasurement_DistinguishStoredFromEffective() {
        var currentDate = new DateOnly(2026, 8, 8);
        var store = new InMemoryBodyMeasurementStore();

        var currentMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m
        );

        var futureMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(1),
            WeightKg: 81m
        );

        store.Save(currentMeasurement);
        store.Save(futureMeasurement);

        var service = new BodyMeasurementHistoryService(store);

        var latestStored = service.GetLatestByDate();

        var latestEffective = service.GetLatestOnOrBefore(currentDate);

        Assert.NotNull(latestStored);
        Assert.Equal(
            futureMeasurement.Id,
            latestStored.Id
        );

        Assert.NotNull(latestEffective);
        Assert.Equal(
            currentMeasurement.Id,
            latestEffective.Id
        );
    }
}
