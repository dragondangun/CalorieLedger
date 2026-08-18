using CalorieLedger.Application.History;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;

namespace CalorieLedger.ViewModels.History;

public partial class DailyJournalWeekDayViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly Action<DateOnly> selectDate;

    public DateOnly Date { get; }
    public bool IsAvailable { get; }
    public bool IsSelected { get; }
    public bool IsToday { get; }
    public bool IsComplete { get; }
    public bool IsEnergyComplete { get; }
    public bool AreMacrosComplete { get; }

    public string DayOfWeekSummary => Date.ToString("ddd", RussianCulture).TrimEnd('.');
    public string DayOfMonthSummary => Date.Day.ToString(RussianCulture);
    public string CaloriesSummary { get; }
    public string ActivitySummary { get; }
    public string StatusSummary { get; }

    public bool HasContextSummary => IsSelected || IsToday;

    public string ContextSummary {
        get {
            if(IsSelected && IsToday) {
                return "сегодня · выбрано";
            }

            if(IsSelected) {
                return "выбрано";
            }

            return IsToday ? "сегодня" : string.Empty;
        }
    }

    public DailyJournalWeekDayViewModel(
        DateOnly date,
        DateOnly currentDate,
        bool isSelected,
        DailyJournalDaySnapshot? snapshot,
        Action<DateOnly> selectDate
    ) {
        ArgumentNullException.ThrowIfNull(selectDate);

        Date = date;
        IsAvailable = date <= currentDate;
        IsSelected = isSelected;
        IsToday = date == currentDate;

        var food = snapshot?.FoodDiary;

        IsComplete = food?.IsComplete == true;
        IsEnergyComplete = food?.IsEnergyComplete == true;
        AreMacrosComplete = food?.AreMacrosComplete == true;

        CaloriesSummary = FormatCalories(snapshot);
        ActivitySummary = snapshot?.ExtraActivityBurnedCaloriesKcal > 0m
            ? $"+{snapshot.ExtraActivityBurnedCaloriesKcal:0} акт."
            : string.Empty;

        StatusSummary = FormatStatus(snapshot);
        this.selectDate = selectDate;
    }

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private void Select() {
        selectDate(Date);
    }

    private bool CanSelect() {
        return IsAvailable;
    }

    private static string FormatCalories(DailyJournalDaySnapshot? snapshot) {
        if(snapshot is null) {
            return "—";
        }

        var food = snapshot.FoodDiary;
        var calories = food.ConsumedTotals.CaloriesKcal ?? 0m;

        if(!food.HasUnknownCalories) {
            return $"{calories:0} ккал";
        }

        return calories > 0m ? $"≥ {calories:0} ккал" : "ккал ?";
    }

    private static string FormatStatus(DailyJournalDaySnapshot? snapshot) {
        if(snapshot is null) {
            return "ещё не наступил";
        }

        var food = snapshot.FoodDiary;

        if(!food.IsComplete) {
            return food.Meals.Count > 0 ? "день открыт" : "нет еды";
        }

        if(!food.IsEnergyComplete) {
            return "калории неполны";
        }

        if(!food.AreMacrosComplete) {
            return "калории полны";
        }

        return "данные полны";
    }
}
