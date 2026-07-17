using CalorieLedger.Application.Adaptive;
using CalorieLedger.Domain.Adaptive;

namespace CalorieLedger.Tests.Application.Adaptive;

public sealed class
    InMemoryAdaptiveEnergyEvaluationStoreTests {
    [Fact]
    public void Save_NewDate_AddsEntry() {
        var store =
            new InMemoryAdaptiveEnergyEvaluationStore();

        var entry =
            CreateEntry(
                new DateOnly(2026, 8, 1),
                AdaptiveEnergyDeviationDirection
                    .DecreaseCalories);

        store.Save(entry);

        var storedEntry =
            Assert.Single(
                store.GetAll());

        Assert.Equal(
            entry,
            storedEntry);
    }

    [Fact]
    public void Save_ExistingDate_ReplacesEntry() {
        var store =
            new InMemoryAdaptiveEnergyEvaluationStore();

        var date =
            new DateOnly(2026, 8, 1);

        store.Save(
            CreateEntry(
                date,
                AdaptiveEnergyDeviationDirection
                    .DecreaseCalories));

        store.Save(
            CreateEntry(
                date,
                AdaptiveEnergyDeviationDirection
                    .IncreaseCalories));

        var storedEntry =
            Assert.Single(
                store.GetAll());

        Assert.Equal(
            AdaptiveEnergyDeviationDirection
                .IncreaseCalories,
            storedEntry.DeviationDirection);
    }

    [Fact]
    public void Clear_RemovesAllEntries() {
        var store =
            new InMemoryAdaptiveEnergyEvaluationStore();

        store.Save(
            CreateEntry(
                new DateOnly(2026, 8, 1),
                AdaptiveEnergyDeviationDirection
                    .DecreaseCalories));

        store.Clear();

        Assert.Empty(
            store.GetAll());
    }

    private static AdaptiveEnergyEvaluationEntry
        CreateEntry(
            DateOnly date,
            AdaptiveEnergyDeviationDirection
                direction) {
        var adjustment =
            direction switch
            {
                AdaptiveEnergyDeviationDirection
                    .IncreaseCalories => 100m,

                AdaptiveEnergyDeviationDirection
                    .DecreaseCalories => -100m,

                _ => 0m
            };

        return new AdaptiveEnergyEvaluationEntry(
            EvaluationDate:
                date,
            AdjustmentStatus:
                AdaptiveEnergyAdjustmentStatus
                    .RecommendationAvailable,
            DeviationDirection:
                direction,
            ObservedWeeklyWeightChangeKg:
                -0.2m,
            TargetWeeklyWeightChangeKg:
                -0.5m,
            RecommendedDailyAdjustmentKcal:
                adjustment,
            RecommendedTargetCaloriesKcal:
                2000m + adjustment);
    }
}