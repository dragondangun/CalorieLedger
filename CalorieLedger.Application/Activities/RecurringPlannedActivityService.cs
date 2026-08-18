using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public sealed class RecurringPlannedActivityService {
    private readonly IRecurringPlannedActivityStore store;

    public RecurringPlannedActivityService(IRecurringPlannedActivityStore store) {
        ArgumentNullException.ThrowIfNull(store);
        this.store = store;
    }

    public IReadOnlyList<RecurringPlannedActivity> GetAll() {
        return store.GetAll();
    }

    public RecurringPlannedActivityDraft CreateNew(DateOnly startDate) {
        return new(
            Id: Guid.NewGuid(),
            StartDate: startDate,
            DayOfWeek: startDate.DayOfWeek,
            IntervalWeeks: 1,
            Name: string.Empty
        );
    }

    public RecurringPlannedActivityDraft? Load(Guid id) {
        var schedule = store.Get(id);

        return schedule is null
            ? null
            : new RecurringPlannedActivityDraft(
                schedule.Id,
                schedule.StartDate,
                schedule.DayOfWeek,
                schedule.IntervalWeeks,
                schedule.Name,
                schedule.PlannedAt,
                schedule.Duration,
                schedule.PresetCode,
                schedule.MetValue,
                schedule.ManualBurnedCaloriesKcal,
                schedule.Note
            );
    }

    public RecurringPlannedActivitySaveResult Save(RecurringPlannedActivityDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = Validate(draft);

        if(errors.Count > 0) {
            return new(false, errors);
        }

        store.Save(
            new RecurringPlannedActivity(
                Id: draft.Id,
                StartDate: draft.StartDate,
                DayOfWeek: draft.DayOfWeek,
                IntervalWeeks: draft.IntervalWeeks,
                Name: draft.Name.Trim(),
                PlannedAt: draft.PlannedAt,
                Duration: draft.Duration,
                PresetCode: draft.PresetCode,
                MetValue: draft.MetValue,
                ManualBurnedCaloriesKcal: draft.ManualBurnedCaloriesKcal,
                Note: string.IsNullOrWhiteSpace(draft.Note) ? null : draft.Note.Trim()
            )
        );

        return new(true, []);
    }

    public bool Delete(Guid id) {
        return store.Delete(id);
    }

    public IReadOnlyList<RecurringPlannedActivityOccurrence> GetOccurrences(DateOnly date) {
        return [
            .. store.GetAll()
                .Where(schedule => IsOccurrence(schedule, date))
                .Where(schedule => store.GetOccurrenceState(schedule.Id, date) is null)
                .Select(schedule => new RecurringPlannedActivityOccurrence(
                    schedule.Id,
                    date,
                    schedule.Name,
                    schedule.PlannedAt,
                    schedule.Duration,
                    schedule.PresetCode,
                    schedule.MetValue,
                    schedule.ManualBurnedCaloriesKcal,
                    schedule.Note
                ))
                .OrderBy(occurrence => occurrence.PlannedAt)
                .ThenBy(occurrence => occurrence.Name)
        ];
    }

    public void CompleteOccurrence(
        Guid scheduleId,
        DateOnly date,
        Guid activityId
    ) {
        store.SaveOccurrenceState(
            new RecurringPlannedActivityOccurrenceState(
                scheduleId,
                date,
                RecurringPlannedActivityOccurrenceStatus.Completed,
                activityId
            )
        );
    }

    public void SkipOccurrence(Guid scheduleId, DateOnly date) {
        store.SaveOccurrenceState(
            new RecurringPlannedActivityOccurrenceState(
                scheduleId,
                date,
                RecurringPlannedActivityOccurrenceStatus.Skipped
            )
        );
    }

    private static bool IsOccurrence(
        RecurringPlannedActivity schedule,
        DateOnly date
    ) {
        if(date < schedule.StartDate || date.DayOfWeek != schedule.DayOfWeek) {
            return false;
        }

        var daysUntilFirstOccurrence =
            ((int)schedule.DayOfWeek - (int)schedule.StartDate.DayOfWeek + 7) % 7;

        var firstOccurrence = schedule.StartDate.AddDays(daysUntilFirstOccurrence);

        if(date < firstOccurrence) {
            return false;
        }

        var daysSinceFirstOccurrence = date.DayNumber - firstOccurrence.DayNumber;
        return daysSinceFirstOccurrence % (schedule.IntervalWeeks * 7) == 0;
    }

    private static IReadOnlyList<RecurringPlannedActivityValidationError> Validate(
        RecurringPlannedActivityDraft draft
    ) {
        var errors = new List<RecurringPlannedActivityValidationError>();

        if(draft.Id == Guid.Empty) {
            errors.Add(RecurringPlannedActivityValidationError.MissingId);
        }

        if(string.IsNullOrWhiteSpace(draft.Name)) {
            errors.Add(RecurringPlannedActivityValidationError.MissingName);
        }

        if(draft.IntervalWeeks <= 0) {
            errors.Add(RecurringPlannedActivityValidationError.InvalidInterval);
        }

        if(draft.Duration is not null && draft.Duration <= TimeSpan.Zero) {
            errors.Add(RecurringPlannedActivityValidationError.InvalidDuration);
        }

        if(draft.PresetCode is not null && draft.MetValue is null or < 1m) {
            errors.Add(RecurringPlannedActivityValidationError.InvalidMetValue);
        }

        if(draft.ManualBurnedCaloriesKcal is not null
            && draft.ManualBurnedCaloriesKcal <= 0m) {
            errors.Add(RecurringPlannedActivityValidationError.InvalidManualBurnedCalories);
        }

        return errors;
    }
}
