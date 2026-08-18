using CalorieLedger.Application.History;

namespace CalorieLedger.ViewModels.History;

public sealed class WeeklyJournalSummaryViewModel:ViewModelBase {
    public int AvailableDayCount { get; }
    public int EnergyCompleteDayCount { get; }
    public int MacroCompleteDayCount { get; }

    public decimal? AverageFoodCaloriesKcal { get; }
    public decimal? AverageExtraActivityBurnedCaloriesKcal { get; }
    public decimal? AverageActivityAdjustedCaloriesKcal { get; }
    public decimal TotalExtraActivityBurnedCaloriesKcal { get; }

    public int WeightMeasurementCount { get; }
    public decimal? FirstWeightKg { get; }
    public decimal? LastWeightKg { get; }
    public decimal? WeightChangeKg { get; }

    public string FoodSummary => AverageFoodCaloriesKcal is decimal calories
        ? $"Среднее потребление: {calories:0} ккал/день"
        : "Среднее потребление пока недоступно";

    public string ActivitySummary => AverageExtraActivityBurnedCaloriesKcal is decimal average
        ? $"Доп. активность: {average:0} ккал/день в среднем · {TotalExtraActivityBurnedCaloriesKcal:0} ккал всего"
        : $"Доп. активность: {TotalExtraActivityBurnedCaloriesKcal:0} ккал всего";

    public string AdjustedCaloriesSummary => AverageActivityAdjustedCaloriesKcal is decimal calories
        ? $"После вычета доп. активности: {calories:0} ккал/день"
        : "Скорректированное среднее пока недоступно";

    public string DataQualitySummary =>
        $"Полная калорийность: {EnergyCompleteDayCount} из {AvailableDayCount} дней · полные БЖУ: {MacroCompleteDayCount} из {AvailableDayCount} дней";

    public string WeightSummary {
        get {
            if(WeightMeasurementCount == 0) {
                return "Вес: измерений за эту неделю нет";
            }

            if(WeightMeasurementCount == 1) {
                return $"Вес: {FirstWeightKg:0.0} кг · одно измерение";
            }

            return $"Вес: {FirstWeightKg:0.0} → {LastWeightKg:0.0} кг · изменение {FormatSigned(WeightChangeKg!.Value)} кг";
        }
    }

    public WeeklyJournalSummaryViewModel(WeeklyJournalSummarySnapshot snapshot) {
        AvailableDayCount = snapshot.AvailableDayCount;
        EnergyCompleteDayCount = snapshot.EnergyCompleteDayCount;
        MacroCompleteDayCount = snapshot.MacroCompleteDayCount;
        AverageFoodCaloriesKcal = snapshot.AverageFoodCaloriesKcal;
        AverageExtraActivityBurnedCaloriesKcal = snapshot.AverageExtraActivityBurnedCaloriesKcal;
        AverageActivityAdjustedCaloriesKcal = snapshot.AverageActivityAdjustedCaloriesKcal;
        TotalExtraActivityBurnedCaloriesKcal = snapshot.TotalExtraActivityBurnedCaloriesKcal;
        WeightMeasurementCount = snapshot.WeightMeasurementCount;
        FirstWeightKg = snapshot.FirstWeightKg;
        LastWeightKg = snapshot.LastWeightKg;
        WeightChangeKg = snapshot.WeightChangeKg;
    }

    private static string FormatSigned(decimal value) {
        return value > 0m ? $"+{value:0.0}" : $"{value:0.0}";
    }
}
