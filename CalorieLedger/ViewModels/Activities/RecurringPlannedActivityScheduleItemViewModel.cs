using CalorieLedger.Domain.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class RecurringPlannedActivityScheduleItemViewModel:ViewModelBase {
    private readonly Action<Guid> edit;
    private readonly Action<Guid> delete;

    public Guid Id { get; }
    public string Name { get; }
    public string ScheduleSummary { get; }
    public string DetailsSummary { get; }

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public bool ArePrimaryActionsVisible => !IsDeleteConfirmationVisible;

    public RecurringPlannedActivityScheduleItemViewModel(
        RecurringPlannedActivity schedule,
        Action<Guid> edit,
        Action<Guid> delete
    ) {
        Id = schedule.Id;
        Name = schedule.Name;
        ScheduleSummary = FormatSchedule(schedule);
        DetailsSummary = FormatDetails(schedule);
        this.edit = edit;
        this.delete = delete;
    }

    [RelayCommand]
    private void Edit() {
        edit(Id);
    }

    [RelayCommand]
    private void Delete() {
        IsDeleteConfirmationVisible = true;
    }

    [RelayCommand]
    private void ConfirmDelete() {
        IsDeleteConfirmationVisible = false;
        delete(Id);
    }

    [RelayCommand]
    private void CancelDelete() {
        IsDeleteConfirmationVisible = false;
    }

    partial void OnIsDeleteConfirmationVisibleChanged(bool value) {
        OnPropertyChanged(nameof(ArePrimaryActionsVisible));
    }

    private static string FormatSchedule(RecurringPlannedActivity schedule) {
        var weekday = RecurringActivityWeekdayOption.All
            .First(option => option.Value == schedule.DayOfWeek)
            .Name
            .ToLowerInvariant();

        return schedule.IntervalWeeks == 1
            ? $"Каждую неделю · {weekday}"
            : $"Каждые {schedule.IntervalWeeks} нед. · {weekday}";
    }

    private static string FormatDetails(RecurringPlannedActivity schedule) {
        var parts = new List<string>();

        if(schedule.PlannedAt is not null) {
            parts.Add(schedule.PlannedAt.Value.ToString("HH:mm"));
        }

        if(schedule.Duration is not null) {
            parts.Add($"{schedule.Duration.Value.TotalMinutes:0} мин");
        }

        if(schedule.MetValue is not null) {
            parts.Add($"{schedule.MetValue:0.#} MET");
        }
        else if(schedule.ManualBurnedCaloriesKcal is not null) {
            parts.Add($"{schedule.ManualBurnedCaloriesKcal:0} ккал");
        }

        return string.Join(" · ", parts);
    }
}
