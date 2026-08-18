using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public sealed class PlannedActivityService {
    private readonly IPlannedActivityStore store;

    public PlannedActivityService(IPlannedActivityStore store) {
        ArgumentNullException.ThrowIfNull(store);
        this.store = store;
    }

    public IReadOnlyList<PlannedActivity> GetAll() {
        return store.GetAll();
    }

    public PlannedActivityDraft CreateNew(DateOnly date) {
        return new(Guid.NewGuid(), date, string.Empty);
    }

    public PlannedActivityDraft? Load(Guid id) {
        var activity = store.Get(id);

        return activity is null
            ? null
            : new PlannedActivityDraft(
                activity.Id,
                activity.Date,
                activity.Name,
                activity.PlannedAt,
                activity.Duration,
                activity.PresetCode,
                activity.MetValue,
                activity.ManualBurnedCaloriesKcal,
                activity.Note
            );
    }

    public PlannedActivitySaveResult Save(PlannedActivityDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = Validate(draft);

        if(errors.Count > 0) {
            return new(false, errors);
        }

        store.Save(
            new PlannedActivity(
                Id: draft.Id,
                Date: draft.Date,
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

    private static IReadOnlyList<PlannedActivityValidationError> Validate(PlannedActivityDraft draft) {
        var errors = new List<PlannedActivityValidationError>();

        if(draft.Id == Guid.Empty) {
            errors.Add(PlannedActivityValidationError.MissingId);
        }

        if(string.IsNullOrWhiteSpace(draft.Name)) {
            errors.Add(PlannedActivityValidationError.MissingName);
        }

        if(draft.Duration is not null && draft.Duration <= TimeSpan.Zero) {
            errors.Add(PlannedActivityValidationError.InvalidDuration);
        }

        if(draft.PresetCode is not null && draft.MetValue is null or < 1m) {
            errors.Add(PlannedActivityValidationError.InvalidMetValue);
        }

        if(draft.ManualBurnedCaloriesKcal is not null && draft.ManualBurnedCaloriesKcal <= 0m) {
            errors.Add(PlannedActivityValidationError.InvalidManualBurnedCalories);
        }

        return errors;
    }
}
