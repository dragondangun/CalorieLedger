using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public sealed class InMemoryRecurringPlannedActivityStore:IRecurringPlannedActivityStore {
    private readonly List<RecurringPlannedActivity> schedules = [];
    private readonly List<RecurringPlannedActivityOccurrenceState> states = [];

    public IReadOnlyList<RecurringPlannedActivity> GetAll() {
        return [
            .. schedules
                .OrderBy(schedule => schedule.DayOfWeek)
                .ThenBy(schedule => schedule.PlannedAt)
                .ThenBy(schedule => schedule.Name)
        ];
    }

    public RecurringPlannedActivity? Get(Guid id) {
        return schedules.FirstOrDefault(schedule => schedule.Id == id);
    }

    public void Save(RecurringPlannedActivity schedule) {
        ArgumentNullException.ThrowIfNull(schedule);

        var index = schedules.FindIndex(existing => existing.Id == schedule.Id);

        if(index >= 0) {
            schedules[index] = schedule;
        }
        else {
            schedules.Add(schedule);
        }
    }

    public bool Delete(Guid id) {
        var removed = schedules.RemoveAll(schedule => schedule.Id == id) > 0;

        if(removed) {
            states.RemoveAll(state => state.ScheduleId == id);
        }

        return removed;
    }

    public RecurringPlannedActivityOccurrenceState? GetOccurrenceState(
        Guid scheduleId,
        DateOnly date
    ) {
        return states.FirstOrDefault(
            state => state.ScheduleId == scheduleId && state.Date == date
        );
    }

    public void SaveOccurrenceState(RecurringPlannedActivityOccurrenceState state) {
        ArgumentNullException.ThrowIfNull(state);

        var index = states.FindIndex(
            existing =>
                existing.ScheduleId == state.ScheduleId
                && existing.Date == state.Date
        );

        if(index >= 0) {
            states[index] = state;
        }
        else {
            states.Add(state);
        }
    }
}
