using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public sealed class PlannedActivityCompletionDraftFactory {
    private readonly ActivityPresetCatalogService presetCatalogService;
    private readonly ActivityEnergySuggestionService energySuggestionService;

    public PlannedActivityCompletionDraftFactory(
        ActivityPresetCatalogService presetCatalogService,
        ActivityEnergySuggestionService energySuggestionService
    ) {
        ArgumentNullException.ThrowIfNull(presetCatalogService);
        ArgumentNullException.ThrowIfNull(energySuggestionService);

        this.presetCatalogService = presetCatalogService;
        this.energySuggestionService = energySuggestionService;
    }

    public ActivityDraft Create(
        DateOnly date,
        string name,
        TimeOnly? startedAt,
        TimeSpan? duration,
        string? presetCode,
        decimal? metValue,
        decimal? manualBurnedCaloriesKcal,
        string? note
    ) {
        var calories = manualBurnedCaloriesKcal;
        ActivityEnergyCalculation? calculation = null;

        if(presetCode is not null
            && metValue is not null
            && duration is not null) {
            var preset = presetCatalogService.Find(presetCode)
                ?? new ActivityPreset(presetCode, name, metValue.Value);

            var suggestion = energySuggestionService.Estimate(
                date,
                preset,
                (decimal)duration.Value.TotalMinutes
            );

            if(suggestion is not null) {
                calories = suggestion.BurnedCaloriesKcal;
                calculation = suggestion.Calculation;
            }
        }

        return new ActivityDraft(
            Id: Guid.NewGuid(),
            Date: date,
            Name: name,
            BurnedCaloriesKcal: calories,
            StartedAt: startedAt,
            Duration: duration,
            Note: note,
            EnergyCalculation: calculation
        );
    }
}
