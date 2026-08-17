using CalorieLedger.Application.Meals;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;

namespace CalorieLedger.ViewModels.Meals;

public partial class FoodDiaryWeekDayViewModel:ViewModelBase {
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

            if(IsToday) {
                return "сегодня";
            }

            return string.Empty;
        }
    }

    public FoodDiaryWeekDayViewModel(
        DateOnly date,
        DateOnly currentDate,
        bool isSelected,
        FoodDiaryDaySnapshot? snapshot,
        Action<DateOnly> selectDate
    ) {
        ArgumentNullException.ThrowIfNull(selectDate);

        Date = date;

        IsAvailable = date <= currentDate;
        IsSelected = isSelected;
        IsToday = date == currentDate;
        IsComplete = snapshot?.IsComplete == true;
        IsEnergyComplete = snapshot?.IsEnergyComplete == true;

        AreMacrosComplete = snapshot?.AreMacrosComplete == true;
        CaloriesSummary = FormatCalories(snapshot);

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

    private static string FormatCalories(FoodDiaryDaySnapshot? snapshot) {
        if(snapshot is null) {
            return "—";
        }

        var calories =snapshot.ConsumedTotals.CaloriesKcal ?? 0m;

        if(!snapshot.HasUnknownCalories) {
            return $"{calories:0} ккал";
        }

        return calories > 0m ? $"≥ {calories:0} ккал" : "ккал неизвестны";
    }

    private static string FormatStatus(FoodDiaryDaySnapshot? snapshot) {
        if(snapshot is null) {
            return "ещё не наступил";
        }

        if(!snapshot.IsComplete) {
            return snapshot.Meals.Count > 0 ? "день открыт" : "нет записей";
        }

        if(!snapshot.IsEnergyComplete) {
            return "калории неполны";
        }

        if(!snapshot.AreMacrosComplete) {
            return "калории полны";
        }

        return "данные полны";
    }
}
