using CalorieLedger.Domain.Profile;
using CalorieLedger.Application.Time;

namespace CalorieLedger.Application.Profiles;

public sealed class BodyMeasurementAwareNutritionProfileProvider:IUserNutritionProfileProvider {
    private readonly IUserNutritionProfileProvider baseProfileProvider;

    private readonly BodyMeasurementHistoryService measurementHistoryService;
    private readonly ICurrentDateProvider currentDateProvider;

    public BodyMeasurementAwareNutritionProfileProvider(
        IUserNutritionProfileProvider baseProfileProvider,
        BodyMeasurementHistoryService measurementHistoryService,
        ICurrentDateProvider currentDateProvider
    ) {
        ArgumentNullException.ThrowIfNull(baseProfileProvider);
        ArgumentNullException.ThrowIfNull(measurementHistoryService);
        ArgumentNullException.ThrowIfNull(currentDateProvider);

        this.baseProfileProvider = baseProfileProvider;
        this.measurementHistoryService = measurementHistoryService;
        this.currentDateProvider = currentDateProvider;
    }

    public UserNutritionProfile GetCurrentProfile() {
        var measurementSnapshot = measurementHistoryService.GetSnapshot(
            currentDateProvider.GetCurrentDate()
        );

        return GetProfile(measurementSnapshot);
    }

    public UserNutritionProfile GetProfile(
        BodyMeasurementHistorySnapshot measurementSnapshot
    ) {
        ArgumentNullException.ThrowIfNull(measurementSnapshot);

        var baseProfile = baseProfileProvider.GetCurrentProfile();

        var latestMeasurement = measurementSnapshot.LatestEffectiveMeasurement;

        if(latestMeasurement is null) {
            return baseProfile;
        }

        var currentBody = baseProfile.Body with {
            WeightKg = latestMeasurement.WeightKg,
            BodyFatPercent = latestMeasurement.BodyFatPercent,
            BoneMassKg = latestMeasurement.BoneMassKg,
            MuscleMassKg = latestMeasurement.MuscleMassKg,
            MusclePercent = latestMeasurement.MusclePercent,
        };

        return baseProfile with {
            Body = currentBody,
        };
    }
}
