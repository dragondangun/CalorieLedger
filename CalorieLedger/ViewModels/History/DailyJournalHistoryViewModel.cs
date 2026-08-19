using CalorieLedger.Application.Activities;
using CalorieLedger.Application.History;
using CalorieLedger.Application.Meals;
using CalorieLedger.ViewModels.Activities;
using CalorieLedger.ViewModels.Meals;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CalorieLedger.ViewModels.History;

public partial class DailyJournalHistoryViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly DailyJournalDaySnapshotProvider snapshotProvider;
    private readonly DateOnly currentDate;
    private readonly Action<DateOnly> addFood;
    private readonly Action<DateOnly> addApproximateFood;
    private readonly Action<Guid> editFood;
    private readonly Action<Guid> deleteFood;
    private readonly Action<DateOnly, bool> setFoodLogComplete;
    private readonly Action<DateOnly> addActivity;
    private readonly Action<Guid> editActivity;
    private readonly Action<Guid> deleteActivity;
    private readonly Action<Guid>? repeatActivity;
    private readonly Action onClosed;
    private readonly WeeklyJournalSummaryProvider weeklySummaryProvider;
    private const int RecentWeekCount = 8;
    private readonly PlannedActivityService? plannedActivityService;
    private readonly Action<Guid>? editPlannedActivity;
    private readonly Action<Guid>? completePlannedActivity;
    private readonly Action<Guid>? deletePlannedActivity;
    private readonly RecurringPlannedActivityService? recurringPlannedActivityService;
    private readonly Action<Guid>? editRecurringPlannedActivity;
    private readonly Action<Guid, DateOnly>? completeRecurringPlannedActivity;
    private readonly Action<Guid, DateOnly>? skipRecurringPlannedActivity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateSummary))]
    [NotifyPropertyChangedFor(nameof(IsToday))]
    [NotifyPropertyChangedFor(nameof(WeekSummary))]
    private DateOnly selectedDate;

    [ObservableProperty]
    private decimal consumedCaloriesKcal;

    [ObservableProperty]
    private decimal proteinG;

    [ObservableProperty]
    private decimal fatG;

    [ObservableProperty]
    private decimal carbsG;

    [ObservableProperty]
    private decimal extraActivityBurnedCaloriesKcal;

    [ObservableProperty]
    private decimal activityAdjustedCaloriesKcal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompletionSummary))]
    [NotifyPropertyChangedFor(nameof(CompletionActionText))]
    private bool isComplete;

    [ObservableProperty]
    private string dataQualitySummary = string.Empty;
    [ObservableProperty]
    private WeeklyJournalSummaryViewModel weeklySummary = null!;
    [ObservableProperty]
    private IReadOnlyList<WeeklyTrendChartPoint> trendChartPoints = [];

    public ObservableCollection<FoodDiaryMealGroupViewModel> MealGroups { get; } = [];
    public ObservableCollection<ActivityItemViewModel> Activities { get; } = [];
    public ObservableCollection<DailyJournalWeekDayViewModel> WeekDays { get; } = [];
    public ObservableCollection<JournalTrendWeekViewModel> RecentWeeks { get; } = [];
    public ObservableCollection<RecurringPlannedActivityOccurrenceItemViewModel> RecurringPlannedActivities { get; } = [];

    public bool HasRecurringPlannedActivities => RecurringPlannedActivities.Count > 0;

    public bool HasMeals => MealGroups.Count > 0;
    public bool HasNoMeals => MealGroups.Count == 0;
    public bool HasActivities => Activities.Count > 0;
    public bool HasNoActivities => Activities.Count == 0;
    public bool IsToday => SelectedDate == currentDate;

    public string DateSummary => IsToday
        ? $"{FormatDate(SelectedDate)} · сегодня"
        : FormatDate(SelectedDate);

    public string WeekSummary {
        get {
            var start = GetWeekStart(SelectedDate);
            return $"{FormatShortDate(start)} — {FormatShortDate(start.AddDays(6))}";
        }
    }

    public string CaloriesSummary => $"{ConsumedCaloriesKcal:0} ккал";
    public string MacrosSummary => $"Б: {ProteinG:0.#} г · Ж: {FatG:0.#} г · У: {CarbsG:0.#} г";

    public string ActivitySummary => ExtraActivityBurnedCaloriesKcal > 0m
        ? $"Дополнительная активность: {ExtraActivityBurnedCaloriesKcal:0} ккал"
        : "Дополнительная активность не указана";

    public string ActivityAdjustedCaloriesSummary =>
        $"С учётом дополнительной активности: {ActivityAdjustedCaloriesKcal:0} ккал";

    public string CompletionSummary => IsComplete ? "День завершён" : "День открыт";
    public string CompletionActionText => IsComplete ? "Открыть день снова" : "Завершить день";

    public string WeekComparisonSummary { get; private set; } = string.Empty;
    public ObservableCollection<PlannedActivityItemViewModel> PlannedActivities { get; } = [];

    public bool HasPlannedActivities => PlannedActivities.Count > 0;
    public bool HasNoPlannedActivities => PlannedActivities.Count == 0;

    public DailyJournalHistoryViewModel(
        DailyJournalDaySnapshotProvider snapshotProvider,
        WeeklyJournalSummaryProvider weeklySummaryProvider,
        DateOnly currentDate,
        Action<DateOnly> addFood,
        Action<DateOnly> addApproximateFood,
        Action<Guid> editFood,
        Action<Guid> deleteFood,
        Action<DateOnly, bool> setFoodLogComplete,
        Action<DateOnly> addActivity,
        Action<Guid> editActivity,
        Action<Guid> deleteActivity,
        Action onClosed,
        Action<Guid>? repeatActivity = null,
        PlannedActivityService? plannedActivityService = null,
        Action<Guid>? editPlannedActivity = null,
        Action<Guid>? completePlannedActivity = null,
        Action<Guid>? deletePlannedActivity = null,
        RecurringPlannedActivityService? recurringPlannedActivityService = null,
        Action<Guid>? editRecurringPlannedActivity = null,
        Action<Guid, DateOnly>? completeRecurringPlannedActivity = null,
        Action<Guid, DateOnly>? skipRecurringPlannedActivity = null
    ) {
        ArgumentNullException.ThrowIfNull(snapshotProvider);
        ArgumentNullException.ThrowIfNull(addFood);
        ArgumentNullException.ThrowIfNull(addApproximateFood);
        ArgumentNullException.ThrowIfNull(editFood);
        ArgumentNullException.ThrowIfNull(deleteFood);
        ArgumentNullException.ThrowIfNull(setFoodLogComplete);
        ArgumentNullException.ThrowIfNull(addActivity);
        ArgumentNullException.ThrowIfNull(editActivity);
        ArgumentNullException.ThrowIfNull(deleteActivity);
        ArgumentNullException.ThrowIfNull(onClosed);
        ArgumentNullException.ThrowIfNull(weeklySummaryProvider);

        this.snapshotProvider = snapshotProvider;
        this.currentDate = currentDate;
        this.addFood = addFood;
        this.addApproximateFood = addApproximateFood;
        this.editFood = editFood;
        this.deleteFood = deleteFood;
        this.setFoodLogComplete = setFoodLogComplete;
        this.addActivity = addActivity;
        this.editActivity = editActivity;
        this.deleteActivity = deleteActivity;
        this.onClosed = onClosed;
        this.weeklySummaryProvider = weeklySummaryProvider;
        this.repeatActivity = repeatActivity;
        this.plannedActivityService = plannedActivityService;
        this.editPlannedActivity = editPlannedActivity;
        this.completePlannedActivity = completePlannedActivity;
        this.deletePlannedActivity = deletePlannedActivity;
        this.recurringPlannedActivityService = recurringPlannedActivityService;
        this.editRecurringPlannedActivity = editRecurringPlannedActivity;
        this.completeRecurringPlannedActivity = completeRecurringPlannedActivity;
        this.skipRecurringPlannedActivity = skipRecurringPlannedActivity;

        selectedDate = currentDate;
        Refresh();
    }

    [RelayCommand]
    private void PreviousDay() {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextDay))]
    private void NextDay() {
        SelectedDate = SelectedDate.AddDays(1);
    }

    private bool CanGoToNextDay() {
        return SelectedDate < currentDate;
    }

    [RelayCommand]
    private void PreviousWeek() {
        SelectedDate = SelectedDate.AddDays(-7);
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextWeek))]
    private void NextWeek() {
        var nextDate = SelectedDate.AddDays(7);
        SelectedDate = nextDate > currentDate ? currentDate : nextDate;
    }

    private bool CanGoToNextWeek() {
        return GetWeekStart(SelectedDate) < GetWeekStart(currentDate);
    }

    [RelayCommand(CanExecute = nameof(CanGoToToday))]
    private void GoToToday() {
        SelectedDate = currentDate;
    }

    private bool CanGoToToday() {
        return SelectedDate != currentDate;
    }

    [RelayCommand]
    private void AddFood() {
        addFood(SelectedDate);
    }

    [RelayCommand]
    private void AddApproximateFood() {
        addApproximateFood(SelectedDate);
    }

    [RelayCommand]
    private void AddActivity() {
        addActivity(SelectedDate);
    }

    [RelayCommand]
    private void ToggleCompletion() {
        setFoodLogComplete(SelectedDate, !IsComplete);
    }

    [RelayCommand]
    private void Close() {
        onClosed();
    }

    [RelayCommand]
    private void SelectWeek(DateOnly weekStartDate) {
        var dayOffset = ((int)SelectedDate.DayOfWeek + 6) % 7;
        var targetDate = weekStartDate.AddDays(dayOffset);

        SelectedDate = targetDate > currentDate ? currentDate : targetDate;
    }

    public void Refresh() {
        RefreshCurrentDay();
        RefreshWeek();
    }

    private void RefreshCurrentDay() {
        var snapshot = snapshotProvider.GetDay(SelectedDate);
        var food = snapshot.FoodDiary;

        ConsumedCaloriesKcal = food.ConsumedTotals.CaloriesKcal ?? 0m;
        ProteinG = food.ConsumedTotals.ProteinG ?? 0m;
        FatG = food.ConsumedTotals.FatG ?? 0m;
        CarbsG = food.ConsumedTotals.CarbsG ?? 0m;
        ExtraActivityBurnedCaloriesKcal = snapshot.ExtraActivityBurnedCaloriesKcal;
        ActivityAdjustedCaloriesKcal = snapshot.ActivityAdjustedCaloriesKcal;
        IsComplete = food.IsComplete;
        DataQualitySummary = FormatDataQuality(food);

        MealGroups.Clear();

        foreach(var meal in food.Meals) {
            MealGroups.Add(FoodDiaryPresentationFactory.CreateMealGroup(
                meal: meal,
                editFood: editFood,
                deleteFood: deleteFood)
            );
        }

        Activities.Clear();

        foreach(var activity in snapshot.Activities) {
            Activities.Add(
                new ActivityItemViewModel(
                    id: activity.Id,
                    name: activity.Name,
                    burnedCaloriesKcal: activity.BurnedCaloriesKcal,
                    startedAt: activity.StartedAt,
                    duration: activity.Duration,
                    note: activity.Note,
                    edit: editActivity,
                    delete: deleteActivity,
                    repeat: repeatActivity
                )
            );
        }

        RefreshRecurringPlannedActivities(snapshot.Date);
        RefreshPlannedActivities(snapshot.Date);
        OnPropertyChanged(nameof(HasMeals));
        OnPropertyChanged(nameof(HasNoMeals));
        OnPropertyChanged(nameof(HasActivities));
        OnPropertyChanged(nameof(HasNoActivities));
        OnPropertyChanged(nameof(CaloriesSummary));
        OnPropertyChanged(nameof(MacrosSummary));
        OnPropertyChanged(nameof(ActivitySummary));
        OnPropertyChanged(nameof(ActivityAdjustedCaloriesSummary));
    }

    private void RefreshWeek() {
        var weekStart = GetWeekStart(SelectedDate);
        var weekEnd = weekStart.AddDays(6);
        var availableEnd = weekEnd < currentDate ? weekEnd : currentDate;
        var snapshots = snapshotProvider.GetRange(weekStart, availableEnd).ToDictionary(x => x.Date);

        WeekDays.Clear();

        for(var offset = 0; offset < 7; offset++) {
            var date = weekStart.AddDays(offset);
            snapshots.TryGetValue(date, out var snapshot);

            WeekDays.Add(
                new DailyJournalWeekDayViewModel(
                    date: date,
                    currentDate: currentDate,
                    isSelected: date == SelectedDate,
                    snapshot: snapshot,
                    selectDate: SelectDate
                )
            );
        }

        var summaries = weeklySummaryProvider.GetRecentWeeks(
            SelectedDate,
            currentDate,
            RecentWeekCount
        );

        WeeklySummary = new WeeklyJournalSummaryViewModel(summaries[^1]);

        RecentWeeks.Clear();

        foreach(var summary in summaries) {
            RecentWeeks.Add(
                new JournalTrendWeekViewModel(
                    summary,
                    summary.WeekStartDate == weekStart,
                    SelectWeek
                )
            );
        }

        TrendChartPoints = [
            .. summaries.Select(
                summary => new WeeklyTrendChartPoint(
                    WeekStartDate: summary.WeekStartDate,
                    Label: $"{summary.WeekStartDate:dd.MM}"
                        + (summary.AvailableEndDate < summary.WeekEndDate ? "*" : ""),
                    FoodCaloriesKcal: summary.AverageFoodCaloriesKcal,
                    AdjustedCaloriesKcal: summary.AverageActivityAdjustedCaloriesKcal,
                    WeightKg: summary.LastWeightKg,
                    IsPartialWeek: summary.AvailableEndDate < summary.WeekEndDate,
                    IsSelectedWeek: summary.WeekStartDate == weekStart
                )
            )
        ];

        WeekComparisonSummary = FormatWeekComparison(summaries);

        OnPropertyChanged(nameof(WeekComparisonSummary));
    }

    private void SelectDate(DateOnly date) {
        if(date <= currentDate) {
            SelectedDate = date;
        }
    }

    partial void OnSelectedDateChanged(DateOnly value) {
        if(value > currentDate) {
            SelectedDate = currentDate;
            return;
        }

        NextDayCommand.NotifyCanExecuteChanged();
        NextWeekCommand.NotifyCanExecuteChanged();
        GoToTodayCommand.NotifyCanExecuteChanged();
        Refresh();
    }

    private static string FormatDataQuality(FoodDiaryDaySnapshot snapshot) {
        if(!snapshot.IsComplete) {
            return "День открыт и пока не используется для недельного среднего или адаптивной оценки.";
        }

        if(snapshot.HasUnknownCalories) {
            return "В завершённом дне есть еда без известной калорийности. Для энергетической статистики день считается неполным.";
        }

        if(!snapshot.AreMacrosComplete) {
            return "Калорийность дня полна. Некоторые БЖУ неизвестны, поэтому день не входит в среднее БЖУ.";
        }

        return "День завершён, данные полны.";
    }

    private static DateOnly GetWeekStart(DateOnly date) {
        return date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
    }

    private static string FormatDate(DateOnly date) {
        return date.ToString("d MMMM yyyy", RussianCulture);
    }

    private static string FormatShortDate(DateOnly date) {
        return date.ToString("d MMM", RussianCulture);
    }

    private static string FormatWeekComparison(
        IReadOnlyList<WeeklyJournalSummarySnapshot> summaries
    ) {
        if(summaries.Count < 2) {
            return string.Empty;
        }

        var previous = summaries[^2].AverageActivityAdjustedCaloriesKcal;
        var current = summaries[^1].AverageActivityAdjustedCaloriesKcal;

        if(previous is null || current is null) {
            return "Для сравнения двух последних недель нужны завершённые дни в обеих неделях.";
        }

        var difference = current.Value - previous.Value;

        return difference switch {
            > 0m => $"К предыдущей неделе: +{difference:0} ккал/день",
            < 0m => $"К предыдущей неделе: {difference:0} ккал/день",
            _ => "Скорректированное среднее совпадает с предыдущей неделей."
        };
    }

    private void RefreshPlannedActivities(DateOnly date) {
        PlannedActivities.Clear();

        if(plannedActivityService is null
            || editPlannedActivity is null
            || completePlannedActivity is null
            || deletePlannedActivity is null
        ) {
            OnPropertyChanged(nameof(HasPlannedActivities));
            OnPropertyChanged(nameof(HasNoPlannedActivities));
            return;
        }

        foreach(var activity in plannedActivityService.Get(date)) {
            PlannedActivities.Add(
                new PlannedActivityItemViewModel(
                    activity,
                    currentDate,
                    editPlannedActivity,
                    completePlannedActivity,
                    deletePlannedActivity,
                    showDate: false
                )
            );
        }

        OnPropertyChanged(nameof(HasPlannedActivities));
        OnPropertyChanged(nameof(HasNoPlannedActivities));
    }

    private void RefreshRecurringPlannedActivities(DateOnly date) {
        RecurringPlannedActivities.Clear();

        if(recurringPlannedActivityService is null
            || editRecurringPlannedActivity is null
            || completeRecurringPlannedActivity is null
            || skipRecurringPlannedActivity is null) {
            OnPropertyChanged(nameof(HasRecurringPlannedActivities));
            return;
        }

        foreach(var occurrence in recurringPlannedActivityService.GetOccurrences(date)) {
            RecurringPlannedActivities.Add(
                new RecurringPlannedActivityOccurrenceItemViewModel(
                    occurrence,
                    currentDate,
                    editRecurringPlannedActivity,
                    completeRecurringPlannedActivity,
                    skipRecurringPlannedActivity
                )
            );
        }

        OnPropertyChanged(nameof(HasRecurringPlannedActivities));
    }
}
