using System.Linq;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class BodyMeasurementAwareNutritionProfileProvider:IUserNutritionProfileProvider {
    private readonly IUserNutritionProfileProvider baseProfileProvider;

    private readonly BodyMeasurementHistoryService measurementHistoryService;

    public BodyMeasurementAwareNutritionProfileProvider(
        IUserNutritionProfileProvider baseProfileProvider,
        BodyMeasurementHistoryService measurementHistoryService) {
        ArgumentNullException.ThrowIfNull(baseProfileProvider);

        ArgumentNullException.ThrowIfNull(measurementHistoryService);

        this.baseProfileProvider = baseProfileProvider;

        this.measurementHistoryService = measurementHistoryService;
    }

    public UserNutritionProfile GetCurrentProfile() {
        var baseProfile = baseProfileProvider.GetCurrentProfile();

        var latestMeasurement = measurementHistoryService
            .GetAll()
            .LastOrDefault();

        if(latestMeasurement is null) {
            return baseProfile;
        }

        var currentBody = baseProfile.Body with {
            WeightKg = latestMeasurement.WeightKg,
            BodyFatPercent = latestMeasurement.BodyFatPercent,
            BoneMassKg = latestMeasurement.BoneMassKg,
            MuscleMassKg = latestMeasurement.MuscleMassKg,
            MusclePercent = latestMeasurement.MusclePercent
        };

        return baseProfile with {
            Body = currentBody
        };
    }
}