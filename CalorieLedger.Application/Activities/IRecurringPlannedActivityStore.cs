using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public interface IRecurringPlannedActivityStore {
    IReadOnlyList<RecurringPlannedActivity> GetAll();
    RecurringPlannedActivity? Get(Guid id);
    void Save(RecurringPlannedActivity schedule);
    bool Delete(Guid id);

    RecurringPlannedActivityOccurrenceState? GetOccurrenceState(
        Guid scheduleId,
        DateOnly date
    );

    void SaveOccurrenceState(RecurringPlannedActivityOccurrenceState state);
}
