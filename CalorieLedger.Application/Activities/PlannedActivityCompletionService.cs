namespace CalorieLedger.Application.Activities;

public sealed class PlannedActivityCompletionService {
    private readonly IPlannedActivityStore plannedActivityStore;
    private readonly PlannedActivityCompletionDraftFactory draftFactory;

    public PlannedActivityCompletionService(
        IPlannedActivityStore plannedActivityStore,
        PlannedActivityCompletionDraftFactory draftFactory
    ) {
        ArgumentNullException.ThrowIfNull(plannedActivityStore);
        ArgumentNullException.ThrowIfNull(draftFactory);

        this.plannedActivityStore = plannedActivityStore;
        this.draftFactory = draftFactory;
    }

    public ActivityDraft? CreateCompletionDraft(Guid planId, DateOnly completionDate) {
        var plan = plannedActivityStore.Get(planId);

        if(plan is null) {
            return null;
        }

        return draftFactory.Create(
            completionDate,
            plan.Name,
            plan.PlannedAt,
            plan.Duration,
            plan.PresetCode,
            plan.MetValue,
            plan.ManualBurnedCaloriesKcal,
            plan.Note
        );
    }
}
