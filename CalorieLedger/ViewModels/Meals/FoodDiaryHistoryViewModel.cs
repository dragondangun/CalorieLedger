using CalorieLedger.Application.Meals;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

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

    public ObservableCollection<FoodDiaryMealGroupViewModel> MealGroups { get; } = [];

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

        RefreshCurrentDay();
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

    public void RefreshCurrentDay() {
        var snapshot = snapshotProvider.GetDay(SelectedDate);

        ConsumedCaloriesKcal = snapshot.ConsumedTotals.CaloriesKcal ?? 0m;
        ProteinG = snapshot.ConsumedTotals.ProteinG ?? 0m;
        FatG = snapshot.ConsumedTotals.FatG ?? 0m;
        CarbsG = snapshot.ConsumedTotals.CarbsG ?? 0m;

        IsComplete = snapshot.IsComplete;

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
        NextDayCommand.NotifyCanExecuteChanged();
        GoToTodayCommand.NotifyCanExecuteChanged();

        RefreshCurrentDay();
    }

    private static string FormatDate(DateOnly date) {
        return date.ToString("d MMMM yyyy", RussianCulture);
    }
}
