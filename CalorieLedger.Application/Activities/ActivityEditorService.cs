using CalorieLedger.Domain.Activities;
using System;
using System.Collections.Generic;

namespace CalorieLedger.Application.Activities;

public sealed class ActivityEditorService {
    private readonly IActivityStore activityStore;

    public ActivityEditorService(IActivityStore activityStore) {
        ArgumentNullException.ThrowIfNull(activityStore);

        this.activityStore = activityStore;
    }

    public ActivityDraft CreateNew(DateOnly date) {
        return new ActivityDraft(
            Id: Guid.NewGuid(),
            Date: date,
            Name: string.Empty,
            BurnedCaloriesKcal: null
        );
    }

    public ActivityDraft? Load(Guid id) {
        var entry = activityStore.Get(id);

        if(entry is null) {
            return null;
        }

        return new ActivityDraft(
            Id: entry.Id,
            Date: entry.Date,
            Name: entry.Name,
            BurnedCaloriesKcal: entry.BurnedCaloriesKcal,
            StartedAt: entry.StartedAt,
            Duration: entry.Duration,
            Note: entry.Note,
            EnergyCalculation: entry.EnergyCalculation
        );
    }

    public ActivitySaveResult Save(
        ActivityDraft draft,
        DateOnly currentDate
    ) {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = Validate(
            draft,
            currentDate
        );

        if(errors.Count > 0) {
            return new ActivitySaveResult(
                IsSuccess: false,
                Errors: errors
            );
        }

        activityStore.Save(
            new ActivityEntry(
                Id: draft.Id,
                Date: draft.Date,
                Name: draft.Name.Trim(),
                BurnedCaloriesKcal: draft.BurnedCaloriesKcal!.Value,
                StartedAt: draft.StartedAt,
                Duration: draft.Duration,
                Note: NormalizeOptionalText(draft.Note),
                EnergyCalculation: draft.EnergyCalculation
            )
        );

        return new ActivitySaveResult(
            IsSuccess: true,
            Errors: []
        );
    }

    public bool Delete(Guid id) {
        return activityStore.Delete(id);
    }

    private static IReadOnlyList<ActivityValidationError> Validate(
        ActivityDraft draft,
        DateOnly currentDate
    ) {
        var errors = new List<ActivityValidationError>();

        if(draft.Id == Guid.Empty) {
            errors.Add(ActivityValidationError.MissingId);
        }

        if(draft.Date > currentDate) {
            errors.Add(ActivityValidationError.FutureDate);
        }

        if(string.IsNullOrWhiteSpace(draft.Name)) {
            errors.Add(ActivityValidationError.MissingName);
        }

        if(draft.BurnedCaloriesKcal is not > 0m) {
            errors.Add(ActivityValidationError.InvalidBurnedCalories);
        }

        if(draft.Duration is TimeSpan duration && duration <= TimeSpan.Zero) {
            errors.Add(ActivityValidationError.InvalidDuration);
        }

        return errors;
    }

    private static string? NormalizeOptionalText(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
