using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Activities;

public sealed class ActivityEnergySuggestionServiceTests {
    [Fact]
    public void Estimate_UsesLatestWeightNotAfterActivityDate() {
        var store = new InMemoryBodyMeasurementStore();

        store.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: new DateOnly(2026, 8, 1),
                WeightKg: 60m
            )
        );

        store.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: new DateOnly(2026, 8, 20),
                WeightKg: 70m
            )
        );

        var service = new ActivityEnergySuggestionService(
            new BodyMeasurementHistoryService(store)
        );

        var preset = new ActivityPreset("test", "Test", 6m);

        var result = service.Estimate(
            new DateOnly(2026, 8, 18),
            preset,
            60m
        );

        Assert.NotNull(result);
        Assert.Equal(300m, result.BurnedCaloriesKcal);
        Assert.Equal(60m, result.Calculation.WeightKg);
        Assert.Equal(6m, result.Calculation.MetValue);
    }

    [Fact]
    public void Estimate_NoEarlierWeight_ReturnsNull() {
        var store = new InMemoryBodyMeasurementStore();

        store.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: new DateOnly(2026, 8, 20),
                WeightKg: 60m
            )
        );

        var service = new ActivityEnergySuggestionService(
            new BodyMeasurementHistoryService(store)
        );

        var result = service.Estimate(
            new DateOnly(2026, 8, 18),
            new ActivityPreset("test", "Test", 6m),
            60m
        );

        Assert.Null(result);
    }
}
