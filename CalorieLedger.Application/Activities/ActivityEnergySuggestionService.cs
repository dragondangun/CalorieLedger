using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Activities;
using System.Linq;

namespace CalorieLedger.Application.Activities;

public sealed class ActivityEnergySuggestionService {
    private readonly BodyMeasurementHistoryService bodyMeasurementHistoryService;

    public ActivityEnergySuggestionService(BodyMeasurementHistoryService bodyMeasurementHistoryService) {
        ArgumentNullException.ThrowIfNull(bodyMeasurementHistoryService);
        this.bodyMeasurementHistoryService = bodyMeasurementHistoryService;
    }

    public ActivityEnergySuggestion? Estimate(
        DateOnly date,
        ActivityPreset preset,
        decimal durationMinutes
    ) {
        ArgumentNullException.ThrowIfNull(preset);

        if(durationMinutes <= 0m) {
            return null;
        }

        var measurement = bodyMeasurementHistoryService.GetAll()
            .Where(measurement => measurement.Date <= date)
            .OrderByDescending(measurement => measurement.Date)
            .FirstOrDefault();

        if(measurement is null) {
            return null;
        }

        var duration = TimeSpan.FromMinutes((double)durationMinutes);
        var calories = ActivityEnergyEstimator.EstimateExtraCalories(
            preset.MetValue,
            measurement.WeightKg,
            duration
        );

        return new ActivityEnergySuggestion(
            calories,
            new ActivityEnergyCalculation(
                PresetCode: preset.Code,
                MetValue: preset.MetValue,
                WeightKg: measurement.WeightKg,
                DurationMinutes: durationMinutes
            )
        );
    }
}
