using CalorieLedger.Application.Adaptive;
using CalorieLedger.Application.Nutrition;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels.Adaptive;

namespace CalorieLedger.Tests.ViewModels.Adaptive;

public sealed class AdaptiveEnergyAssessmentPresentationProviderTests {
    [Fact]
    public void GetCurrent_RequestsCurrentAdaptiveWindow() {
        var asOfDate = new DateOnly(2026, 7, 28);

        var intakeHistoryProvider = new TrackingDailyEnergyIntakeHistoryProvider();

        var provider = CreateProvider(
            intakeHistoryProvider
        );

        _ = provider.GetCurrent(
            CreateMeasurementSnapshot(
                asOfDate
            )
        );

        Assert.Equal(
            asOfDate.AddDays(-13),
            intakeHistoryProvider.LastStartDate
        );

        Assert.Equal(
            asOfDate,
            intakeHistoryProvider.LastEndDate
        );
    }

    [Fact]
    public void GetCurrent_InsufficientMeasurementHistory_ReturnsUnavailable() {
        var asOfDate = new DateOnly(2026, 7, 14);

        var provider = CreateProvider(
            new TrackingDailyEnergyIntakeHistoryProvider()
        );

        var result = provider.GetCurrent(
            CreateMeasurementSnapshot(
                asOfDate,
                dayCount: 1
            )
        );

        Assert.Equal(
            AdaptiveEnergyAssessmentState.Unavailable,
            result.State
        );

        Assert.Null(
            result.SuggestedStrategy
        );
    }

    [Fact]
    public void GetCurrent_FirstDeviation_ReturnsObservationRequired() {
        var asOfDate = new DateOnly(2026, 7, 14);

        var provider = CreateProvider(
            new TrackingDailyEnergyIntakeHistoryProvider()
        );

        var result = provider.GetCurrent(
            CreateMeasurementSnapshot(
                asOfDate
            )
        );

        Assert.Equal(
            AdaptiveEnergyAssessmentState.ObservationRequired,
            result.State
        );

        Assert.Null(result.SuggestedStrategy);
    }

    [Fact]
    public void GetCurrent_SecondConsistentDeviation_ReturnsAdjustmentSuggestion() {
        var firstEvaluationDate = new DateOnly(2026, 7, 14);

        var provider = CreateProvider(
            new TrackingDailyEnergyIntakeHistoryProvider()
        );

        _ = provider.GetCurrent(
            CreateMeasurementSnapshot(
                firstEvaluationDate
            )
        );

        var result = provider.GetCurrent(
            CreateMeasurementSnapshot(
                firstEvaluationDate.AddDays(7)
            )
        );

        Assert.Equal(
            AdaptiveEnergyAssessmentState.AdjustmentSuggested,
            result.State
        );

        var suggestion = Assert.IsType<AdaptiveEnergyStrategySuggestion>(
            result.SuggestedStrategy
        );

        Assert.Equal(
            EnergyStrategyMode.WeightChangePerWeek,
            suggestion.Mode
        );

        Assert.True(suggestion.Value > 0m);

        Assert.Contains(
            "ккал",
            result.Recommendation
        );
    }

    private static AdaptiveEnergyAssessmentPresentationProvider CreateProvider(
        IDailyEnergyIntakeHistoryProvider intakeHistoryProvider
    ) {
        var profileStore = new InMemoryUserNutritionProfileStore(
            CreateProfile()
        );

        var measurementHistoryService = new BodyMeasurementHistoryService(
            new InMemoryBodyMeasurementStore()
        );

        var profileProvider = new BodyMeasurementAwareNutritionProfileProvider(
            profileStore,
            measurementHistoryService,
            new FixedCurrentDateProvider(
                new DateOnly(2026, 7, 1)
            )
        );

        return new AdaptiveEnergyAssessmentPresentationProvider(
            new AdaptiveEnergyAssessmentService(
                new InMemoryAdaptiveEnergyEvaluationStore()
            ),
            intakeHistoryProvider,
            profileProvider
        );
    }

    private static UserNutritionProfile CreateProfile() {
        return new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Test user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Male,
                AgeYears: 30,
                HeightCm: 180m,
                WeightKg: 80m,
                BodyFatPercent: 20m
            ),
            LifestyleActivityLevel: LifestyleActivityLevel.Sedentary,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.LoseWeight,
                Strategy: EnergyStrategy.FromWeightChangePerWeek(
                    0.5m
                )
            )
        );
    }

    private static BodyMeasurementHistorySnapshot CreateMeasurementSnapshot(
        DateOnly asOfDate,
        int dayCount = 14
    ) {
        var startDate = asOfDate.AddDays(
            -(dayCount - 1)
        );

        return new BodyMeasurementHistorySnapshot(
            asOfDate: asOfDate,
            allMeasurements: [
                .. Enumerable.Range(0, dayCount).Select(
                    day => new BodyMeasurementEntry(
                        Id: Guid.NewGuid(),
                        Date: startDate.AddDays(day),
                        WeightKg: 80m - day * 0.1m
                    )
                ),
            ]
        );
    }

    private sealed class TrackingDailyEnergyIntakeHistoryProvider:IDailyEnergyIntakeHistoryProvider {
        public DateOnly? LastStartDate { get; private set; }
        public DateOnly? LastEndDate { get; private set; }

        public IReadOnlyList<DailyEnergyIntakeEntry> GetEntries(
            DateOnly startDate,
            DateOnly endDate
        ) {
            LastStartDate = startDate;
            LastEndDate = endDate;

            var dayCount = endDate.DayNumber - startDate.DayNumber + 1;

            return [
                .. Enumerable.Range(0, dayCount).Select(
                    day => new DailyEnergyIntakeEntry(
                        Date: startDate.AddDays(day),
                        CaloriesKcal: 2200m,
                        IsComplete: true
                    )
                ),
            ];
        }
    }
}
