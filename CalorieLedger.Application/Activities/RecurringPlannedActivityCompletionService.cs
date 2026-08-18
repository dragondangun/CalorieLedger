namespace CalorieLedger.Application.Activities;

public sealed class RecurringPlannedActivityCompletionService {
    private readonly RecurringPlannedActivityService recurringService;
    private readonly PlannedActivityCompletionDraftFactory draftFactory;

    public RecurringPlannedActivityCompletionService(
        RecurringPlannedActivityService recurringService,
        PlannedActivityCompletionDraftFactory draftFactory
    ) {
        ArgumentNullException.ThrowIfNull(recurringService);
        ArgumentNullException.ThrowIfNull(draftFactory);

        this.recurringService = recurringService;
        this.draftFactory = draftFactory;
    }

    public ActivityDraft? CreateCompletionDraft(
        Guid scheduleId,
        DateOnly occurrenceDate
    ) {
        var occurrence = recurringService.GetOccurrences(occurrenceDate)
            .FirstOrDefault(occurrence => occurrence.ScheduleId == scheduleId);

        if(occurrence is null) {
            return null;
        }

        return draftFactory.Create(
            occurrenceDate,
            occurrence.Name,
            occurrence.PlannedAt,
            occurrence.Duration,
            occurrence.PresetCode,
            occurrence.MetValue,
            occurrence.ManualBurnedCaloriesKcal,
            occurrence.Note
        );
    }
}
