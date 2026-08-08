using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Time;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Tests.Profiles;

public sealed class
    BodyMeasurementAwareNutritionProfileProviderTests {
    [Fact]
    public void GetCurrentProfile_WithoutMeasurements_ReturnsBaseProfile() {
        var baseProfile = CreateBaseProfile();

        var historyService = CreateHistoryService();

        var currentDate = new DateOnly(2026, 8, 8);

        var baseProfileProvider = new FixedUserNutritionProfileProvider(baseProfile);

        var provider = new BodyMeasurementAwareNutritionProfileProvider(
            baseProfileProvider,
            historyService,
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetCurrentProfile();

        Assert.Equal(baseProfile, result);
    }

    [Fact]
    public void GetCurrentProfile_UsesLatestMeasurement() {
        var baseProfile = CreateBaseProfile();

        var historyService = CreateHistoryService();

        var earlierDate = new DateOnly(2026, 7, 18);

        var latestDate = new DateOnly(2026, 7, 19);

        historyService.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: earlierDate,
                WeightKg: 79m,
                BodyFatPercent: 19m
            ),
            latestDate
        );

        historyService.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: latestDate,
                WeightKg: 78m,
                BodyFatPercent: 18m,
                BoneMassKg: 3.1m,
                MuscleMassKg: 34m,
                MusclePercent: 43.59m
            ),
            latestDate
        );

        var currentDate = new DateOnly(2026, 8, 8);

        var baseProfileProvider = new FixedUserNutritionProfileProvider(baseProfile);

        var provider = new BodyMeasurementAwareNutritionProfileProvider(
            baseProfileProvider,
            historyService,
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetCurrentProfile();

        Assert.Equal(
            78m,
            result.Body.WeightKg
        );

        Assert.Equal(
            18m,
            result.Body.BodyFatPercent
        );

        Assert.Equal(
            3.1m,
            result.Body.BoneMassKg
        );

        Assert.Equal(
            34m,
            result.Body.MuscleMassKg
        );

        Assert.Equal(
            43.59m,
            result.Body.MusclePercent
        );

        Assert.Equal(
            baseProfile.Body.Sex,
            result.Body.Sex
        );

        Assert.Equal(
            baseProfile.Body.AgeYears,
            result.Body.AgeYears
        );

        Assert.Equal(
            baseProfile.Body.HeightCm,
            result.Body.HeightCm
        );
    }

    [Fact]
    public void GetCurrentProfile_OlderMeasurementAddedLater_DoesNotReplaceLatest() {
        var historyService = CreateHistoryService();

        var currentDate = new DateOnly(2026, 7, 19);

        historyService.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                WeightKg: 78m
            ),
            currentDate
        );

        historyService.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: new DateOnly(2026, 7, 10),
                WeightKg: 82m
            ),
            currentDate
        );

        var baseProfile = CreateBaseProfile();
        var baseProfileProvider = new FixedUserNutritionProfileProvider(baseProfile);

        var provider = new BodyMeasurementAwareNutritionProfileProvider(
            baseProfileProvider,
            historyService,
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetCurrentProfile();

        Assert.Equal(78m, result.Body.WeightKg);
    }

    [Fact]
    public void GetCurrentProfile_AfterLatestDeleted_UsesPreviousMeasurement() {
        var historyService = CreateHistoryService();

        var currentDate = new DateOnly(2026, 7, 19);

        var earlierEntry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 18),
            WeightKg: 79m
        );

        var latestEntry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 78m
        );

        historyService.Save(
            earlierEntry,
            currentDate
        );

        historyService.Save(
            latestEntry,
            currentDate
        );

        historyService.Delete(latestEntry.Id);

        var baseProfile = CreateBaseProfile();
        var baseProfileProvider = new FixedUserNutritionProfileProvider(baseProfile);

        var provider = new BodyMeasurementAwareNutritionProfileProvider(
            baseProfileProvider,
            historyService,
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetCurrentProfile();

        Assert.Equal(79m, result.Body.WeightKg);
    }

    [Fact]
    public void GetCurrentProfile_WeightOnlyMeasurement_ClearsOldComposition() {
        var baseProfile = CreateBaseProfile();

        var historyService = CreateHistoryService();

        var currentDate = new DateOnly(2026, 7, 19);

        historyService.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                WeightKg: 78m
            ),
            currentDate
        );

        var baseProfileProvider = new FixedUserNutritionProfileProvider(baseProfile);

        var provider = new BodyMeasurementAwareNutritionProfileProvider(
            baseProfileProvider,
            historyService,
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetCurrentProfile();

        Assert.Equal(78m, result.Body.WeightKg);

        Assert.Null(result.Body.BodyFatPercent);

        Assert.Null(result.Body.BoneMassKg);

        Assert.Null(result.Body.MuscleMassKg);

        Assert.Null(result.Body.MusclePercent);
    }

    [Fact]
    public void GetProfile_FutureMeasurement_DoesNotOverrideProfile() {
        var currentDate = new DateOnly(2026, 8, 8);

        var profile = CreateProfile(
            weightKg: 70m
        );

        var profileStore = new InMemoryUserNutritionProfileStore(profile);

        var measurementStore = new InMemoryBodyMeasurementStore();

        measurementStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate.AddDays(1),
                WeightKg: 80m
            )
        );

        var historyService = new BodyMeasurementHistoryService(measurementStore);

        var provider = new BodyMeasurementAwareNutritionProfileProvider(
            profileStore,
            historyService,
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetCurrentProfile();

        Assert.Equal(70m, result.Body.WeightKg);
    }

    [Fact]
    public void GetProfile_CurrentDateMeasurement_OverridesProfile() {
        var currentDate = new DateOnly(2026, 8, 8);

        var profile = CreateProfile(
            weightKg: 70m
        );

        var profileStore = new InMemoryUserNutritionProfileStore(profile);

        var measurementStore = new InMemoryBodyMeasurementStore();

        measurementStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate,
                WeightKg: 80m
            )
        );

        var historyService = new BodyMeasurementHistoryService(measurementStore);

        var provider = new BodyMeasurementAwareNutritionProfileProvider(
            profileStore,
            historyService,
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetCurrentProfile();

        Assert.Equal(80m, result.Body.WeightKg);
    }

    [Fact]
    public void GetProfile_FutureAndPastMeasurements_UsesLatestNonFutureMeasurement() {
        var currentDate = new DateOnly(2026, 8, 8);

        var profile = CreateProfile(
            weightKg: 70m
        );

        var profileStore = new InMemoryUserNutritionProfileStore(profile);

        var measurementStore = new InMemoryBodyMeasurementStore();

        measurementStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate.AddDays(-1),
                WeightKg: 75m
            )
        );

        measurementStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: currentDate.AddDays(1),
                WeightKg: 80m
            )
        );

        var historyService = new BodyMeasurementHistoryService(measurementStore);

        var provider = new BodyMeasurementAwareNutritionProfileProvider(
            profileStore,
            historyService,
            new FixedCurrentDateProvider(currentDate)
        );

        var result = provider.GetCurrentProfile();

        Assert.Equal(
            75m,
            result.Body.WeightKg
        );
    }

    private static BodyMeasurementHistoryService CreateHistoryService() {
        return new BodyMeasurementHistoryService(new InMemoryBodyMeasurementStore());
    }

    private static UserNutritionProfile CreateBaseProfile() {
        return new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Test user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Male,
                AgeYears: 30,
                HeightCm: 180m,
                WeightKg: 80m,
                BodyFatPercent: 20m,
                BoneMassKg: 3.2m,
                MuscleMassKg: 35m,
                MusclePercent: 43.75m
            ),
            LifestyleActivityLevel: LifestyleActivityLevel.Sedentary,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain,
                Strategy: EnergyStrategy.FromBalancePercent(0m)
            )
        );
    }

    private sealed class TestProfileProvider:IUserNutritionProfileProvider {
        private readonly UserNutritionProfile profile;

        public TestProfileProvider(UserNutritionProfile profile) {
            this.profile = profile;
        }

        public UserNutritionProfile GetCurrentProfile() {
            return profile;
        }
    }

    private static UserNutritionProfile CreateProfile(decimal weightKg) {
        return new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Test user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Female,
                AgeYears: 30,
                HeightCm: 170m,
                WeightKg: weightKg,
                BodyFatPercent: null,
                BoneMassKg: null
            ),
            LifestyleActivityLevel: LifestyleActivityLevel.Sedentary,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain
            )
        );
    }

    private sealed class FixedCurrentDateProvider:ICurrentDateProvider {
        private readonly DateOnly currentDate;

        public FixedCurrentDateProvider(DateOnly currentDate) {
            this.currentDate = currentDate;
        }

        public DateOnly GetCurrentDate() {
            return currentDate;
        }
    }

    private sealed class FixedUserNutritionProfileProvider:IUserNutritionProfileProvider {
        private readonly UserNutritionProfile profile;

        public FixedUserNutritionProfileProvider(UserNutritionProfile profile) {
            ArgumentNullException.ThrowIfNull(profile);

            this.profile = profile;
        }

        public UserNutritionProfile GetCurrentProfile() {
            return profile;
        }
    }
}
