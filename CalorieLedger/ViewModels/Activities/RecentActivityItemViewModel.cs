using CalorieLedger.Domain.Activities;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CalorieLedger.ViewModels.Activities;

public sealed partial class RecentActivityItemViewModel:ViewModelBase {
    private readonly Action<Guid> apply;

    public Guid Id { get; }
    public string Name { get; }
    public string DateSummary { get; }
    public string DetailsSummary { get; }

    public RecentActivityItemViewModel(ActivityEntry activity, Action<Guid> apply) {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(apply);

        Id = activity.Id;
        Name = activity.Name;
        DateSummary = activity.Date.ToString("dd.MM");
        DetailsSummary = FormatDetails(activity);
        this.apply = apply;
    }

    [RelayCommand]
    private void Apply() {
        apply(Id);
    }

    private static string FormatDetails(ActivityEntry activity) {
        if(activity.Duration is null) {
            return $"{activity.BurnedCaloriesKcal:0} ккал";
        }

        var duration = activity.Duration.Value.TotalHours >= 1
            ? $"{activity.Duration.Value.TotalHours:0.#} ч"
            : $"{activity.Duration.Value.TotalMinutes:0} мин";

        return $"{duration} · {activity.BurnedCaloriesKcal:0} ккал";
    }
}
