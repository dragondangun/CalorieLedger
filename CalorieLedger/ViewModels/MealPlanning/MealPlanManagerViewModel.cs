using CalorieLedger.Application.MealPlanning;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CalorieLedger.ViewModels.MealPlanning;

public sealed partial class MealPlanManagerViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly MealPlanService mealPlanService;
    private readonly DateOnly currentDate;
    private readonly Action onClosed;
    private readonly Action<DateOnly, MealGroupRole, MealPlanItem>? logFood;

    [ObservableProperty]
    private DateOnly selectedDate;

    [ObservableProperty]
    private string dateSummary = string.Empty;

    [ObservableProperty]
    private string nutritionSummary = string.Empty;

    [ObservableProperty]
    private bool hasPlan;

    [ObservableProperty]
    private bool isDeleteConfirmationVisible;

    public ObservableCollection<MealPlanMealViewModel> Meals { get; } = [];

    public ObservableCollection<MealPlanDayLinkViewModel> PlannedDays { get; } = [];

    public bool HasNoPlan => !HasPlan;

    public bool HasPlannedDays => PlannedDays.Count > 0;

    public bool HasNoPlannedDays => PlannedDays.Count == 0;

    public bool CanGoToPreviousDay => SelectedDate > currentDate;

    public bool CanDeleteSelectedDay => HasPlan;

    public MealPlanManagerViewModel(
        MealPlanService mealPlanService,
        DateOnly currentDate,
        Action onClosed,
        Action<DateOnly, MealGroupRole, MealPlanItem>? logFood = null
    ) {
        ArgumentNullException.ThrowIfNull(mealPlanService);
        ArgumentNullException.ThrowIfNull(onClosed);

        this.mealPlanService = mealPlanService;
        this.currentDate = currentDate;
        this.onClosed = onClosed;
        this.logFood = logFood;
        selectedDate = currentDate;

        Refresh();
    }

    public void Refresh() {
        RefreshPlannedDays();
        RefreshSelectedDay();
    }

    [RelayCommand]
    private void PreviousDay() {
        if(!CanGoToPreviousDay) {
            return;
        }

        SelectedDate = SelectedDate.AddDays(-1);
    }

    [RelayCommand]
    private void NextDay() {
        SelectedDate = SelectedDate.AddDays(1);
    }

    [RelayCommand]
    private void GoToToday() {
        SelectedDate = currentDate;
    }

    [RelayCommand]
    private void RequestDeleteSelectedDay() {
        if(!HasPlan) {
            return;
        }

        IsDeleteConfirmationVisible = true;
    }

    [RelayCommand]
    private void CancelDeleteSelectedDay() {
        IsDeleteConfirmationVisible = false;
    }

    [RelayCommand]
    private void ConfirmDeleteSelectedDay() {
        if(!HasPlan) {
            IsDeleteConfirmationVisible = false;
            return;
        }

        mealPlanService.Delete(SelectedDate);
        IsDeleteConfirmationVisible = false;
        Refresh();
    }

    [RelayCommand]
    private void Close() {
        onClosed();
    }

    partial void OnSelectedDateChanged(DateOnly value) {
        IsDeleteConfirmationVisible = false;
        RefreshSelectedDay();
        RefreshPlannedDays();
        OnPropertyChanged(nameof(CanGoToPreviousDay));
        PreviousDayCommand.NotifyCanExecuteChanged();
    }

    private void RefreshSelectedDay() {
        DateSummary = FormatDate(SelectedDate);
        Meals.Clear();

        var day = mealPlanService.Get(SelectedDate, SelectedDate).SingleOrDefault();
        HasPlan = day is not null;

        if(day is not null) {
            foreach(var meal in day.Meals) {
                Meals.Add(
                    new MealPlanMealViewModel(
                        meal,
                        SelectedDate,
                        SelectedDate == currentDate && logFood is not null,
                        LogFood
                    )
                );
            }

            NutritionSummary = FormatDayNutrition(day);
        }
        else {
            NutritionSummary = "На эту дату план питания не сохранён.";
        }

        OnPropertyChanged(nameof(HasNoPlan));
        OnPropertyChanged(nameof(CanDeleteSelectedDay));
        RequestDeleteSelectedDayCommand.NotifyCanExecuteChanged();
    }

    private void RefreshPlannedDays() {
        PlannedDays.Clear();

        foreach(var day in mealPlanService.GetAll().Where(day => day.Date >= currentDate)) {
            PlannedDays.Add(
                new MealPlanDayLinkViewModel(
                    day.Date,
                    day.Date == SelectedDate,
                    SelectDate
                )
            );
        }

        OnPropertyChanged(nameof(HasPlannedDays));
        OnPropertyChanged(nameof(HasNoPlannedDays));
    }

    private void SelectDate(DateOnly date) {
        SelectedDate = date;
    }

    private void LogFood(
        DateOnly date,
        MealGroupRole mealRole,
        MealPlanItem item
    ) {
        logFood?.Invoke(
            date,
            mealRole,
            item
        );
    }

    private static string FormatDate(DateOnly date) {
        var text = date.ToString("dddd, d MMMM yyyy", RussianCulture);
        return char.ToUpper(text[0], RussianCulture) + text[1..];
    }

    private static string FormatDayNutrition(MealPlanDay day) {
        var items = day.Meals.SelectMany(meal => meal.Items).ToArray();

        var calories = SumComplete(items, item => item.Nutrition.CaloriesKcal);
        var protein = SumComplete(items, item => item.Nutrition.ProteinG);
        var fat = SumComplete(items, item => item.Nutrition.FatG);
        var carbs = SumComplete(items, item => item.Nutrition.CarbsG);

        return $"КБЖУ плана: {FormatNutritionValue(calories, "ккал")} · "
            + $"Б {FormatNutritionValue(protein, "г")} · "
            + $"Ж {FormatNutritionValue(fat, "г")} · "
            + $"У {FormatNutritionValue(carbs, "г")}";
    }

    private static decimal? SumComplete(
        IReadOnlyList<MealPlanItem> items,
        Func<MealPlanItem, decimal?> selector
    ) {
        decimal sum = 0m;

        foreach(var item in items) {
            var value = selector(item);

            if(value is null) {
                return null;
            }

            sum += value.Value;
        }

        return sum;
    }

    private static string FormatNutritionValue(decimal? value, string unit) {
        return value is null
            ? "?"
            : $"{value.Value:0.##} {unit}";
    }
}

public sealed class MealPlanDayLinkViewModel {
    private readonly Action<DateOnly> select;

    public DateOnly Date { get; }

    public string DateSummary { get; }

    public bool IsSelected { get; }

    public IRelayCommand SelectCommand { get; }

    public MealPlanDayLinkViewModel(
        DateOnly date,
        bool isSelected,
        Action<DateOnly> select
    ) {
        ArgumentNullException.ThrowIfNull(select);

        Date = date;
        IsSelected = isSelected;
        this.select = select;
        var summary = date.ToString("ddd, dd.MM", CultureInfo.GetCultureInfo("ru-RU"));
        DateSummary = IsSelected ? $"● {summary}" : summary;
        SelectCommand = new RelayCommand(Select);
    }

    private void Select() {
        select(Date);
    }
}

public sealed class MealPlanMealViewModel {
    public string Name { get; }

    public string RoleSummary { get; }

    public string TimeSummary { get; }

    public string? Note { get; }

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public ObservableCollection<MealPlanItemViewModel> Items { get; } = [];

    public MealPlanMealViewModel(
        MealPlanMeal meal,
        DateOnly date,
        bool canLogFood,
        Action<DateOnly, MealGroupRole, MealPlanItem> logFood
    ) {
        ArgumentNullException.ThrowIfNull(meal);
        ArgumentNullException.ThrowIfNull(logFood);

        Name = meal.Name;
        RoleSummary = FormatRole(meal.Role);
        TimeSummary = meal.Time is null
            ? "Время не указано"
            : meal.Time.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
        Note = meal.Note;

        foreach(var item in meal.Items) {
            Items.Add(
                new MealPlanItemViewModel(
                    item,
                    date,
                    meal.Role,
                    canLogFood,
                    logFood
                )
            );
        }
    }

    private static string FormatRole(MealGroupRole role) {
        return role switch {
            MealGroupRole.Breakfast => "Завтрак",
            MealGroupRole.Lunch => "Обед",
            MealGroupRole.Dinner => "Ужин",
            MealGroupRole.Snack => "Перекус",
            MealGroupRole.Custom => "Другой приём пищи",
            _ => "Приём пищи",
        };
    }
}

public sealed class MealPlanItemViewModel {
    private readonly MealPlanItem item;
    private readonly DateOnly date;
    private readonly MealGroupRole mealRole;
    private readonly Action<DateOnly, MealGroupRole, MealPlanItem> logFood;

    public string Name { get; }

    public string QuantitySummary { get; }

    public string NutritionSummary { get; }

    public string SourceSummary { get; }

    public bool HasFridgeSource { get; }

    public string? Note { get; }

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public bool CanLogFood { get; }

    public IRelayCommand LogFoodCommand { get; }

    public MealPlanItemViewModel(
        MealPlanItem item,
        DateOnly date,
        MealGroupRole mealRole,
        bool canLogFood,
        Action<DateOnly, MealGroupRole, MealPlanItem> logFood
    ) {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(logFood);

        this.item = item;
        this.date = date;
        this.mealRole = mealRole;
        this.logFood = logFood;

        Name = item.Name;
        QuantitySummary = FormatQuantity(item.Quantity);
        NutritionSummary = FormatNutrition(item.Nutrition);
        HasFridgeSource = item.FridgeItemId is not null;
        SourceSummary = HasFridgeSource ? "из холодильника" : string.Empty;
        Note = item.Note;
        CanLogFood = canLogFood;
        LogFoodCommand = new RelayCommand(
            LogFood,
            () => CanLogFood
        );
    }

    private void LogFood() {
        if(!CanLogFood) {
            return;
        }

        logFood(
            date,
            mealRole,
            item
        );
    }

    private static string FormatQuantity(FoodQuantity quantity) {
        var unit = quantity.Unit switch {
            FoodUnit.Gram => "г",
            FoodUnit.Milliliter => "мл",
            FoodUnit.Piece => "шт.",
            FoodUnit.Portion => "порц.",
            _ => quantity.Unit.ToString(),
        };

        return $"{quantity.Value:0.##} {unit}";
    }

    private static string FormatNutrition(NutritionTotals nutrition) {
        return $"{FormatValue(nutrition.CaloriesKcal, "ккал")} · "
            + $"Б {FormatValue(nutrition.ProteinG, "г")} · "
            + $"Ж {FormatValue(nutrition.FatG, "г")} · "
            + $"У {FormatValue(nutrition.CarbsG, "г")}";
    }

    private static string FormatValue(decimal? value, string unit) {
        return value is null
            ? "?"
            : $"{value.Value:0.##} {unit}";
    }
}
