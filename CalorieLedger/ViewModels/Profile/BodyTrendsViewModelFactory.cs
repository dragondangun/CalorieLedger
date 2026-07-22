using CalorieLedger.Domain.Profile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalorieLedger.ViewModels.Profile;

public static class BodyTrendsViewModelFactory {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static BodyTrendsViewModel Create(IReadOnlyCollection<BodyMeasurementEntry> measurements, DateOnly asOfDate) {
        ArgumentNullException.ThrowIfNull(measurements);

        var orderedMeasurements = measurements.OrderBy(measurement => measurement.Date).ThenBy(measurement => measurement.Id).ToArray();

        var weightResult = BodyWeightTrendCalculator.Calculate(orderedMeasurements, asOfDate);

        var bodyFatResult = BodyFatTrendCalculator.Calculate(orderedMeasurements, asOfDate);

        return new BodyTrendsViewModel(
            weightTrend: CreateWeightTrend(weightResult),
            bodyFatTrend: CreateBodyFatTrend(bodyFatResult));
    }

    private static BodyTrendCardViewModel CreateWeightTrend(BodyWeightTrendResult result) {
        if(result.Status != BodyWeightTrendStatus.Available
            || result.AverageWeightKg is null
            || result.EstimatedWeeklyChangeKg is null) {
            return BodyTrendCardViewModel.CreateUnavailable(
                title: "Вес",
                statusSummary: "Нужно не менее 8 дней с измерениями веса за последние 14 дней.");
        }

        var weeklyChange = result.EstimatedWeeklyChangeKg.Value;

        var direction = GetDirection(weeklyChange);

        return BodyTrendCardViewModel.CreateAvailable(
            title: "Вес",
            currentValueSummary: $"Среднее: {FormatDecimal(result.AverageWeightKg.Value, decimals: 2)} кг",
            changeSummary: GetDirectionSummary(direction),
            rateSummary: $"Темп: {FormatSigned(weeklyChange, " кг/нед.")}",
            periodSummary: FormatPeriod(result.WindowStartDate, result.WindowEndDate, result.MeasurementDayCount),
            direction: direction);
    }

    private static BodyTrendCardViewModel CreateBodyFatTrend(BodyFatTrendResult result) {
        if(result.Status != BodyFatTrendStatus.Available
            || result.AverageBodyFatPercent is null
            || result.EstimatedWeeklyChangePercentagePoints is null) {
            return BodyTrendCardViewModel.CreateUnavailable(
                title: "Процент жира",
                statusSummary: "Нужно не менее 8 дней с измерениями процента жира и период охвата не менее 21 дня.");
        }

        var weeklyChange = result.EstimatedWeeklyChangePercentagePoints.Value;

        var direction = GetDirection(weeklyChange);

        return BodyTrendCardViewModel.CreateAvailable(
            title: "Процент жира",
            currentValueSummary: $"Среднее: {FormatDecimal(result.AverageBodyFatPercent.Value, decimals: 2)}%",
            changeSummary: GetDirectionSummary(direction),
            rateSummary: $"Темп: {FormatSigned(weeklyChange, " п.п./нед.")}",
            periodSummary: FormatBodyFatPeriod(result),
            direction: direction);
    }

    private static BodyTrendDirection GetDirection(decimal weeklyChange) {
        return weeklyChange switch
        {
            < 0m => BodyTrendDirection.Decreasing,
            > 0m => BodyTrendDirection.Increasing,
            _ => BodyTrendDirection.Stable
        };
    }

    private static string GetDirectionSummary(BodyTrendDirection direction) {
        return direction switch
        {
            BodyTrendDirection.Decreasing => "Направление: снижение",
            BodyTrendDirection.Increasing => "Направление: рост",
            BodyTrendDirection.Stable => "Направление: без изменения",
            _ => "Направление не определено"
        };
    }

    private static string FormatPeriod(DateOnly startDate, DateOnly endDate, int measurementDayCount) {
        return $"{FormatDate(startDate)}–{FormatDate(endDate)}; дней с измерениями: {measurementDayCount}";
    }

    private static string FormatBodyFatPeriod(BodyFatTrendResult result) {
        return $"{FormatDate(result.WindowStartDate)}–{FormatDate(result.WindowEndDate)}; дней с измерениями: {result.MeasurementDayCount}; охват: {result.CoveredDaySpan} дн.";
    }

    private static string FormatSigned(decimal value, string suffix) {
        var sign = value switch {
            > 0m => "+",
            < 0m => "−",
            _ => string.Empty
        };

        return $"{sign}{FormatDecimal(Math.Abs(value), decimals: 2)}{suffix}";
    }

    private static string FormatDecimal(decimal value, int decimals) {
        var format = decimals switch {
            0 => "0",
            1 => "0.0",
            2 => "0.00",
            _ => throw new ArgumentOutOfRangeException(nameof(decimals))
        };

        return value.ToString(format, RussianCulture);
    }

    private static string FormatDate(DateOnly date) {
        return date.ToString("dd.MM.yyyy", RussianCulture);
    }
}