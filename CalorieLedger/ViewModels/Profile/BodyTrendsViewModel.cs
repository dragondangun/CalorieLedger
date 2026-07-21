using System;

namespace CalorieLedger.ViewModels.Profile;

public sealed class BodyTrendsViewModel:ViewModelBase {
    public BodyTrendCardViewModel WeightTrend { get; }

    public BodyTrendCardViewModel BodyFatTrend { get; }

    public bool HasAnyAvailableTrend => WeightTrend.IsAvailable || BodyFatTrend.IsAvailable;

    public BodyTrendsViewModel(
        BodyTrendCardViewModel weightTrend,
        BodyTrendCardViewModel bodyFatTrend) {
        ArgumentNullException.ThrowIfNull(weightTrend);

        ArgumentNullException.ThrowIfNull(bodyFatTrend);

        WeightTrend = weightTrend;
        BodyFatTrend = bodyFatTrend;
    }

    public static BodyTrendsViewModel
        CreateUnavailable() {
        return new BodyTrendsViewModel(
            weightTrend: BodyTrendCardViewModel.CreateUnavailable(
                    title: "Вес",
                    statusSummary: "Недостаточно измерений для расчёта тренда."),

            bodyFatTrend: BodyTrendCardViewModel.CreateUnavailable(
                        title: "Процент жира",
                        statusSummary: "Недостаточно измерений процента жира."));
    }
}