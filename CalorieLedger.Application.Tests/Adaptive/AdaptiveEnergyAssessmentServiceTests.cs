using CalorieLedger.Application.Adaptive;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Adaptive;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Tests.Application.Adaptive;

public sealed class
    AdaptiveEnergyAssessmentServiceTests {
    [Fact]
    public void Evaluate_FirstDeviation_StoresEvaluationAndWaits() {
        var store = new InMemoryAdaptiveEnergyEvaluationStore();

        var service = new AdaptiveEnergyAssessmentService(store);

        var startDate = new DateOnly(2026, 7, 1);

        var result = service.Evaluate(
            measurementSnapshot: CreateMeasurementSnapshot(
                startDate,
                dayCount: 21,
                asOfDate: startDate.AddDays(13)
            ),
            intakeEntries: CreateIntakeEntries(
                startDate,
                dayCount: 21
            ),
            goal: CreateWeightLossGoal(),
            currentTargetCaloriesKcal:2100m
        );

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus.AwaitingConsistentDeviation,
            result.Recommendation.Status
        );

        Assert.False(result.HasRecommendation);

        Assert.Single(store.GetAll());
    }

    [Fact]
    public void Evaluate_SecondMatchingDeviation_ReturnsRecommendation() {
        var store = new InMemoryAdaptiveEnergyEvaluationStore();

        var service = new AdaptiveEnergyAssessmentService(store);

        var startDate = new DateOnly(2026, 7, 1);

        var bodyMeasurements = CreateBodyMeasurements(
            startDate,
            dayCount: 21
        );

        var intakeEntries = CreateIntakeEntries(
            startDate,
            dayCount: 21
        );

        service.Evaluate(
            CreateMeasurementSnapshot(
                startDate,
                dayCount: 21,
                asOfDate: startDate.AddDays(13)
            ),
            intakeEntries,
            CreateWeightLossGoal(),
            currentTargetCaloriesKcal: 2100m
        );

        var result = service.Evaluate(
            CreateMeasurementSnapshot(
                startDate,
                dayCount: 21,
                asOfDate: startDate.AddDays(20)
            ),
            intakeEntries,
            CreateWeightLossGoal(),
            currentTargetCaloriesKcal: 2100m
        );

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus.RecommendationAvailable,
            result.Recommendation.Status
        );

        Assert.True(result.HasRecommendation);

        Assert.Equal(
            2,
            result.Recommendation.ConsecutiveDeviationCount
        );

        Assert.Equal(
            2,
            store.GetAll().Count
        );
    }

    [Fact]
    public void Evaluate_SameDate_ReplacesEntryWithoutIncreasingSequence() {
        var store = new InMemoryAdaptiveEnergyEvaluationStore();

        var service = new AdaptiveEnergyAssessmentService(store);

        var startDate = new DateOnly(2026, 7, 1);

        var evaluationDate = startDate.AddDays(13);

        var measurementSnapshot = CreateMeasurementSnapshot(
            startDate,
            dayCount: 21,
            asOfDate: evaluationDate
        );

        var intakeEntries = CreateIntakeEntries(
            startDate,
            dayCount: 21
        );

        service.Evaluate(
            measurementSnapshot,
            intakeEntries,
            CreateWeightLossGoal(),
            currentTargetCaloriesKcal: 2100m
        );

        var secondResult = service.Evaluate(
            measurementSnapshot,
            intakeEntries,
            CreateWeightLossGoal(),
            currentTargetCaloriesKcal: 2100m
        );

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus.AwaitingConsistentDeviation,
            secondResult.Recommendation.Status
        );

        Assert.Equal(
            1,
            secondResult.Recommendation.ConsecutiveDeviationCount
        );

        Assert.Single(store.GetAll());
    }

    [Fact]
    public void Evaluate_TooSoon_DoesNotStoreSecondEntry() {
        var store = new InMemoryAdaptiveEnergyEvaluationStore();

        var service = new AdaptiveEnergyAssessmentService(store);

        var startDate = new DateOnly(2026, 7, 1);

        var bodyMeasurements = CreateBodyMeasurements(
            startDate,
            dayCount: 21
        );

        var intakeEntries = CreateIntakeEntries(
            startDate,
            dayCount: 21
        );

        service.Evaluate(
            CreateMeasurementSnapshot(
                startDate,
                dayCount: 21,
                asOfDate: startDate.AddDays(13)
            ),
            intakeEntries,
            CreateWeightLossGoal(),
            currentTargetCaloriesKcal: 2100m
        );

        var result = service.Evaluate(
            CreateMeasurementSnapshot(
                startDate,
                dayCount: 21,
                asOfDate: startDate.AddDays(18)
            ),
            intakeEntries,
            CreateWeightLossGoal(),
            currentTargetCaloriesKcal: 2100m
        );

        Assert.Equal(
            AdaptiveEnergyRecommendationStatus.EvaluationTooSoon,
            result.Recommendation.Status
        );

        Assert.Single(store.GetAll());
    }

    [Fact]
    public void ResetHistory_RemovesPreviousEvaluations() {
        var store = new InMemoryAdaptiveEnergyEvaluationStore();

        var service = new AdaptiveEnergyAssessmentService(store);

        var startDate = new DateOnly(2026, 7, 1);

        service.Evaluate(
            CreateMeasurementSnapshot(
                startDate,
                dayCount: 21,
                asOfDate: startDate.AddDays(13)
            ),
            CreateIntakeEntries(
                startDate,
                dayCount: 21
            ),
            CreateWeightLossGoal(),
            currentTargetCaloriesKcal: 2100m
        );

        service.ResetHistory();

        Assert.Empty(store.GetAll());
    }

    private static NutritionGoal CreateWeightLossGoal() {
        return new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            Strategy: EnergyStrategy.FromWeightChangePerWeek(0.5m)
        );
    }

    private static BodyMeasurementEntry[] CreateBodyMeasurements(DateOnly startDate, int dayCount) {
        return Enumerable.Range(0, dayCount).Select(
            day => new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: startDate.AddDays(day),
                WeightKg: 80m - day * 0.1m)
        ).ToArray();
    }

    private static DailyEnergyIntakeEntry[] CreateIntakeEntries(DateOnly startDate, int dayCount) {
        return Enumerable.Range(0, dayCount).Select(
            day => new DailyEnergyIntakeEntry(
                Date: startDate.AddDays(day),
                CaloriesKcal: 2200m,
                IsComplete: true)
        ).ToArray();
    }

    private static BodyMeasurementHistorySnapshot CreateMeasurementSnapshot(
        DateOnly startDate,
        int dayCount,
        DateOnly asOfDate
    ) {
        return new BodyMeasurementHistorySnapshot(
            asOfDate: asOfDate,
            allMeasurements: CreateBodyMeasurements(
                startDate,
                dayCount
            )
        );
    }
}
