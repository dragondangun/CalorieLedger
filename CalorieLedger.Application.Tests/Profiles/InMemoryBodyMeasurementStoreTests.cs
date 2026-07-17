using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class
    InMemoryBodyMeasurementStoreTests {
    [Fact]
    public void Save_NewEntry_AddsEntry() {
        var store =
            new InMemoryBodyMeasurementStore();

        var entry =
            CreateEntry(
                id: Guid.NewGuid(),
                date:
                    new DateOnly(
                        2026,
                        7,
                        1),
                weightKg: 80m);

        store.Save(entry);

        var savedEntry =
            Assert.Single(
                store.GetAll());

        Assert.Equal(
            entry,
            savedEntry);
    }

    [Fact]
    public void Save_ExistingId_ReplacesEntry() {
        var store =
            new InMemoryBodyMeasurementStore();

        var id =
            Guid.NewGuid();

        store.Save(
            CreateEntry(
                id,
                new DateOnly(
                    2026,
                    7,
                    1),
                80m));

        store.Save(
            CreateEntry(
                id,
                new DateOnly(
                    2026,
                    7,
                    2),
                79.8m));

        var savedEntry =
            Assert.Single(
                store.GetAll());

        Assert.Equal(
            new DateOnly(
                2026,
                7,
                2),
            savedEntry.Date);

        Assert.Equal(
            79.8m,
            savedEntry.WeightKg);
    }

    [Fact]
    public void Save_DifferentIdsOnSameDate_KeepsBothEntries() {
        var store =
            new InMemoryBodyMeasurementStore();

        var date =
            new DateOnly(
                2026,
                7,
                1);

        store.Save(
            CreateEntry(
                Guid.NewGuid(),
                date,
                80m));

        store.Save(
            CreateEntry(
                Guid.NewGuid(),
                date,
                79.8m));

        Assert.Equal(
            2,
            store.GetAll().Count);
    }

    [Fact]
    public void GetAll_ReturnsEntriesOrderedByDate() {
        var store =
            new InMemoryBodyMeasurementStore();

        store.Save(
            CreateEntry(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    7,
                    3),
                79.7m));

        store.Save(
            CreateEntry(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    7,
                    1),
                80m));

        store.Save(
            CreateEntry(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    7,
                    2),
                79.9m));

        var entries =
            store.GetAll();

        Assert.Equal(
            new DateOnly(
                2026,
                7,
                1),
            entries[0].Date);

        Assert.Equal(
            new DateOnly(
                2026,
                7,
                2),
            entries[1].Date);

        Assert.Equal(
            new DateOnly(
                2026,
                7,
                3),
            entries[2].Date);
    }

    [Fact]
    public void Delete_ExistingEntry_RemovesEntry() {
        var store =
            new InMemoryBodyMeasurementStore();

        var id =
            Guid.NewGuid();

        store.Save(
            CreateEntry(
                id,
                new DateOnly(
                    2026,
                    7,
                    1),
                80m));

        var deleted =
            store.Delete(id);

        Assert.True(deleted);
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Delete_UnknownId_ReturnsFalse() {
        var store =
            new InMemoryBodyMeasurementStore();

        var deleted =
            store.Delete(
                Guid.NewGuid());

        Assert.False(deleted);
    }

    private static BodyMeasurementEntry
        CreateEntry(
            Guid id,
            DateOnly date,
            decimal weightKg) {
        return new BodyMeasurementEntry(
            Id: id,
            Date: date,
            WeightKg: weightKg);
    }
}