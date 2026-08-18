using CalorieLedger.Domain.Activities;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class RecurringPlannedActivityOccurrenceItemViewModel:ViewModelBase {
    private readonly Action<Guid> editSchedule;
    private readonly Action<Guid, DateOnly> complete;
    private readonly Action<Guid, DateOnly> skip;

    public Guid ScheduleId { get; }
    public DateOnly Date { get; }
    public string Name { get; }
    public string DetailsSummary { get; }
    public bool CanBeCompleted { get; }

    public RecurringPlannedActivityOccurrenceItemViewModel(
        RecurringPlannedActivityOccurrence occurrence,
        DateOnly currentDate,
        Action<Guid> editSchedule,
        Action<Guid, DateOnly> complete,
        Action<Guid, DateOnly> skip
    ) {
        ScheduleId = occurrence.ScheduleId;
        Date = occurrence.Date;
        Name = occurrence.Name;
        DetailsSummary = FormatDetails(occurrence);
        CanBeCompleted = occurrence.Date <= currentDate;

        this.editSchedule = editSchedule;
        this.complete = complete;
        this.skip = skip;
    }

    [RelayCommand]
    private void EditSchedule() {
        editSchedule(ScheduleId);
    }

    [RelayCommand(CanExecute = nameof(CanComplete))]
    private void Complete() {
        complete(ScheduleId, Date);
    }

    private bool CanComplete() {
        return CanBeCompleted;
    }

    [RelayCommand]
    private void Skip() {
        skip(ScheduleId, Date);
    }

    private static string FormatDetails(RecurringPlannedActivityOccurrence occurrence) {
        var parts = new List<string>();

        if(occurrence.PlannedAt is not null) {
            parts.Add(occurrence.PlannedAt.Value.ToString("HH:mm"));
        }

        if(occurrence.Duration is not null) {
            parts.Add($"{occurrence.Duration.Value.TotalMinutes:0} мин");
        }

        if(occurrence.MetValue is not null) {
            parts.Add($"{occurrence.MetValue:0.#} MET");
        }
        else if(occurrence.ManualBurnedCaloriesKcal is not null) {
            parts.Add($"{occurrence.ManualBurnedCaloriesKcal:0} ккал");
        }

        return string.Join(" · ", parts);
    }
}
