using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public sealed class ActivityRepeatService {
    private readonly IActivityStore activityStore;
    private readonly ActivityPresetCatalogService presetCatalogService;
    private readonly ActivityEnergySuggestionService energySuggestionService;

    public ActivityRepeatService(
        IActivityStore activityStore,
        ActivityPresetCatalogService presetCatalogService,
        ActivityEnergySuggestionService energySuggestionService
    ) {
        ArgumentNullException.ThrowIfNull(activityStore);
        ArgumentNullException.ThrowIfNull(presetCatalogService);
        ArgumentNullException.ThrowIfNull(energySuggestionService);

        this.activityStore = activityStore;
        this.presetCatalogService = presetCatalogService;
        this.energySuggestionService = energySuggestionService;
    }

    public ActivityDraft? CreateDraft(Guid sourceId, DateOnly targetDate) {
        var source = activityStore.Get(sourceId);

        if(source is null) {
            return null;
        }

        if(source.EnergyCalculation is null || source.Duration is null) {
            return CreateManualDraft(source, targetDate);
        }

        var calculation = source.EnergyCalculation;
        var preset = presetCatalogService.Find(calculation.PresetCode)
            ?? new ActivityPreset(
                calculation.PresetCode,
                source.Name,
                calculation.MetValue
            );

        var durationMinutes = (decimal)source.Duration.Value.TotalMinutes;
        var suggestion = energySuggestionService.Estimate(
            targetDate,
            preset,
            durationMinutes
        );

        if(suggestion is null) {
            return CreateManualDraft(source, targetDate);
        }

        return new ActivityDraft(
            Id: Guid.NewGuid(),
            Date: targetDate,
            Name: source.Name,
            BurnedCaloriesKcal: suggestion.BurnedCaloriesKcal,
            StartedAt: null,
            Duration: source.Duration,
            Note: null,
            EnergyCalculation: suggestion.Calculation
        );
    }

    private static ActivityDraft CreateManualDraft(
        ActivityEntry source,
        DateOnly targetDate
    ) {
        return new ActivityDraft(
            Id: Guid.NewGuid(),
            Date: targetDate,
            Name: source.Name,
            BurnedCaloriesKcal: source.BurnedCaloriesKcal,
            StartedAt: null,
            Duration: source.Duration,
            Note: null,
            EnergyCalculation: null
        );
    }
}
