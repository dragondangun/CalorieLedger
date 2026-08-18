using CalorieLedger.Application.History;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;

namespace CalorieLedger.ViewModels.History;

public sealed partial class JournalTrendWeekViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly Action<DateOnly> selectWeek;

    public DateOnly WeekStartDate { get; }
    public DateOnly AvailableEndDate { get; }
    public bool IsSelectedWeek { get; }

    public string DateRangeSummary => $"{WeekStartDate.ToString("d MMM", RussianCulture)} — {AvailableEndDate.ToString("d MMM", RussianCulture)}";

    public string FoodSummary { get; }
    public string AdjustedCaloriesSummary { get; }
    public string ActivitySummary { get; }
    public string WeightSummary { get; }
    public string DataQualitySummary { get; }
    public string SelectionSummary => IsSelectedWeek ? "выбранная неделя" : string.Empty;

    public JournalTrendWeekViewModel(
        WeeklyJournalSummarySnapshot snapshot,
        bool isSelectedWeek,
        Action<DateOnly> selectWeek
    ) {
        ArgumentNullException.ThrowIfNull(selectWeek);

        WeekStartDate = snapshot.WeekStartDate;
        AvailableEndDate = snapshot.AvailableEndDate;
        IsSelectedWeek = isSelectedWeek;
        this.selectWeek = selectWeek;

        FoodSummary = snapshot.AverageFoodCaloriesKcal is decimal foodCalories
            ? $"{foodCalories:0} ккал/день"
            : "еда: —";

        AdjustedCaloriesSummary = snapshot.AverageActivityAdjustedCaloriesKcal is decimal adjustedCalories
            ? $"{adjustedCalories:0} ккал/день после активности"
            : "с учётом активности: —";

        ActivitySummary = $"{snapshot.TotalExtraActivityBurnedCaloriesKcal:0} ккал доп. активности";
        WeightSummary = FormatWeight(snapshot);
        DataQualitySummary = $"{snapshot.EnergyCompleteDayCount}/{snapshot.AvailableDayCount} дней по калориям";
    }

    [RelayCommand]
    private void Select() {
        selectWeek(WeekStartDate);
    }

    private static string FormatWeight(WeeklyJournalSummarySnapshot snapshot) {
        if(snapshot.WeightMeasurementCount == 0) {
            return "вес: —";
        }

        if(snapshot.WeightMeasurementCount == 1) {
            return $"вес: {snapshot.FirstWeightKg:0.0} кг";
        }

        return $"вес: {snapshot.FirstWeightKg:0.0} → {snapshot.LastWeightKg:0.0} кг ({FormatSigned(snapshot.WeightChangeKg!.Value)} кг)";
    }

    private static string FormatSigned(decimal value) {
        return value > 0m ? $"+{value:0.0}" : $"{value:0.0}";
    }
}
