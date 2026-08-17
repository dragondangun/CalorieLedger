using CalorieLedger.Application.Meals;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CalorieLedger.ViewModels.Meals;

public partial class FoodDiaryHistoryViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly FoodDiaryDaySnapshotProvider snapshotProvider;
    private readonly DateOnly currentDate;
    private readonly Action<DateOnly> addFood;
    private readonly Action<DateOnly> addApproximateFood;
    private readonly Action<Guid> editFood;
    private readonly Action<Guid> deleteFood;
    private readonly Action<DateOnly, bool> setFoodLogComplete;
    private readonly Action onClosed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateSummary))]
    [NotifyPropertyChangedFor(nameof(IsToday))]
    [NotifyPropertyChangedFor(nameof(WeekSummary))]
    private DateOnly selectedDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CaloriesSummary))]
    private decimal consumedCaloriesKcal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal proteinG;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal fatG;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal carbsG;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompletionSummary))]
    [NotifyPropertyChangedFor(nameof(CompletionActionText))]
    private bool isComplete;

    [ObservableProperty]
    private string dataQualitySummary = string.Empty;

    public ObservableCollection<FoodDiaryMealGroupViewModel> MealGroups { get; } = [];

    public ObservableCollection<FoodDiaryWeekDayViewModel> WeekDays { get; } = [];

    public string WeekSummary {
        get {
            var weekStart = GetWeekStart(SelectedDate);

            var weekEnd = weekStart.AddDays(6);

            return $"{FormatShortDate(weekStart)} — {FormatShortDate(weekEnd)}";
        }
    }

    public string WeekDataQualitySummary { get; private set; } = string.Empty;

    public bool HasMeals => MealGroups.Count > 0;

    public bool HasNoMeals => MealGroups.Count == 0;

    public bool IsToday => SelectedDate == currentDate;

    public string DateSummary => IsToday ? $"{FormatDate(SelectedDate)} · сегодня" : FormatDate(SelectedDate);

    public string CaloriesSummary => $"{ConsumedCaloriesKcal:0} ккал";

    public string MacrosSummary => $"Б: {ProteinG:0.#} г · Ж: {FatG:0.#} г · У: {CarbsG:0.#} г";

    public string CompletionSummary => IsComplete ? "День завершён" : "День открыт";

    public string CompletionActionText => IsComplete ? "Открыть день снова" : "Завершить день";

    public FoodDiaryHistoryViewModel(
        FoodDiaryDaySnapshotProvider snapshotProvider,
        DateOnly currentDate,
        Action<DateOnly> addFood,
        Action<DateOnly> addApproximateFood,
        Action<Guid> editFood,
        Action<Guid> deleteFood,
        Action<DateOnly, bool> setFoodLogComplete,
        Action onClosed
    ) {
        ArgumentNullException.ThrowIfNull(snapshotProvider);
        ArgumentNullException.ThrowIfNull(addFood);
        ArgumentNullException.ThrowIfNull(addApproximateFood);
        ArgumentNullException.ThrowIfNull(editFood);
        ArgumentNullException.ThrowIfNull(deleteFood);
        ArgumentNullException.ThrowIfNull(setFoodLogComplete);
        ArgumentNullException.ThrowIfNull(onClosed);

        this.snapshotProvider = snapshotProvider;

        this.currentDate = currentDate;

        this.addFood = addFood;

        this.addApproximateFood = addApproximateFood;

        this.editFood = editFood;

        this.deleteFood = deleteFood;

        this.setFoodLogComplete = setFoodLogComplete;

        this.onClosed = onClosed;

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
    private void ToggleCompletion() {
        setFoodLogComplete(
            SelectedDate,
            !IsComplete
        );
    }

    [RelayCommand]
    private void Close() {
        onClosed();
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

    public void Refresh() {
        RefreshCurrentDay();
        RefreshWeek();
    }
    private void RefreshWeek() {
        var weekStart = GetWeekStart(SelectedDate);

        var weekEnd = weekStart.AddDays(6);

        var availableEnd = weekEnd < currentDate ? weekEnd : currentDate;

        var snapshots = snapshotProvider
            .GetRange(weekStart, availableEnd)
            .ToDictionary(snapshot => snapshot.Date);

        WeekDays.Clear();

        for(var offset = 0; offset < 7; offset++) {
            var date = weekStart.AddDays(offset);

            snapshots.TryGetValue(date, out var snapshot);

            WeekDays.Add(
                new FoodDiaryWeekDayViewModel(
                    date: date,
                    currentDate: currentDate,
                    isSelected: date == SelectedDate,
                    snapshot: snapshot,
                    selectDate: SelectDate
                )
            );
        }

        var availableDays = WeekDays.Count(day => day.IsAvailable);

        var energyCompleteDays = WeekDays.Count(day => day.IsEnergyComplete);

        var macroCompleteDays = WeekDays.Count(
            day => day.AreMacrosComplete
        );

        WeekDataQualitySummary = $"Полная калорийность: {energyCompleteDays} из {availableDays} дней · полные БЖУ: {macroCompleteDays} из {availableDays} дней";

        OnPropertyChanged(nameof(WeekDataQualitySummary));
    }

    private void SelectDate(DateOnly date) {
        if(date > currentDate) {
            return;
        }

        SelectedDate = date;
    }

    public void RefreshCurrentDay() {
        var snapshot = snapshotProvider.GetDay(SelectedDate);

        ConsumedCaloriesKcal = snapshot.ConsumedTotals.CaloriesKcal ?? 0m;
        ProteinG = snapshot.ConsumedTotals.ProteinG ?? 0m;
        FatG = snapshot.ConsumedTotals.FatG ?? 0m;
        CarbsG = snapshot.ConsumedTotals.CarbsG ?? 0m;

        IsComplete = snapshot.IsComplete;
        DataQualitySummary = FormatDataQuality(snapshot);
        MealGroups.Clear();

        foreach(var meal in snapshot.Meals) {
            MealGroups.Add(
                FoodDiaryPresentationFactory.CreateMealGroup(
                    meal: meal,
                    editFood: editFood,
                    deleteFood: deleteFood
                )
            );
        }

        OnPropertyChanged(nameof(HasMeals));

        OnPropertyChanged(nameof(HasNoMeals));
    }

    partial void OnSelectedDateChanged(DateOnly value) {
        if(value > currentDate) {
            SelectedDate = currentDate;

            return;
        }

        NextDayCommand.NotifyCanExecuteChanged();
        NextWeekCommand.NotifyCanExecuteChanged();
        GoToTodayCommand.NotifyCanExecuteChanged();

        RefreshCurrentDay();
        RefreshWeek();
    }

    private static string FormatDate(DateOnly date) {
        return date.ToString("d MMMM yyyy", RussianCulture);
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
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;

        return date.AddDays(-daysSinceMonday);
    }

    private static string FormatShortDate(DateOnly date) {
        return date.ToString("d MMM", RussianCulture);
    }
}
