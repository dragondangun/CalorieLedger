using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public sealed class PlannedActivityCompletionService {
    private readonly IPlannedActivityStore plannedActivityStore;
    private readonly ActivityPresetCatalogService presetCatalogService;
    private readonly ActivityEnergySuggestionService energySuggestionService;

    public PlannedActivityCompletionService(
        IPlannedActivityStore plannedActivityStore,
        ActivityPresetCatalogService presetCatalogService,
        ActivityEnergySuggestionService energySuggestionService
    ) {
        ArgumentNullException.ThrowIfNull(plannedActivityStore);
        ArgumentNullException.ThrowIfNull(presetCatalogService);
        ArgumentNullException.ThrowIfNull(energySuggestionService);

        this.plannedActivityStore = plannedActivityStore;
        this.presetCatalogService = presetCatalogService;
        this.energySuggestionService = energySuggestionService;
    }

    public ActivityDraft? CreateCompletionDraft(Guid planId, DateOnly completionDate) {
        var plan = plannedActivityStore.Get(planId);

        if(plan is null) {
            return null;
        }

        var calories = plan.ManualBurnedCaloriesKcal;
        ActivityEnergyCalculation? calculation = null;

        if(plan.PresetCode is not null
            && plan.MetValue is not null
            && plan.Duration is not null) {
            var preset = presetCatalogService.Find(plan.PresetCode)
                ?? new ActivityPreset(plan.PresetCode, plan.Name, plan.MetValue.Value);

            var suggestion = energySuggestionService.Estimate(
                completionDate,
                preset,
                (decimal)plan.Duration.Value.TotalMinutes
            );

            if(suggestion is not null) {
                calories = suggestion.BurnedCaloriesKcal;
                calculation = suggestion.Calculation;
            }
        }

        return new ActivityDraft(
            Id: Guid.NewGuid(),
            Date: completionDate,
            Name: plan.Name,
            BurnedCaloriesKcal: calories,
            StartedAt: plan.PlannedAt,
            Duration: plan.Duration,
            Note: plan.Note,
            EnergyCalculation: calculation
        );
    }
}
