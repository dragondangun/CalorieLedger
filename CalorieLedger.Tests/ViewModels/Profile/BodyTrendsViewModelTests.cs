using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class BodyTrendsViewModelTests {
    [Fact]
    public void CreateUnavailable_CreatesTwoUnavailableCards() {
        var viewModel = BodyTrendsViewModel.CreateUnavailable();

        Assert.False(viewModel.WeightTrend.IsAvailable);

        Assert.False(viewModel.BodyFatTrend.IsAvailable);

        Assert.False(viewModel.HasAnyAvailableTrend);
    }

    [Fact]
    public void Constructor_OneAvailableTrend_HasAvailableTrend() {
        var weightTrend = BodyTrendCardViewModel.CreateAvailable(
            title: "Вес",
            currentValueSummary: "79,5 кг",
            changeSummary: "−0,8 кг",
            rateSummary: "−0,4 кг/нед.",
            periodSummary: "за 14 дней",
            direction: BodyTrendDirection.Decreasing);

        var bodyFatTrend = BodyTrendCardViewModel.CreateUnavailable(
            title: "Процент жира",
            statusSummary: "Недостаточно измерений.");

        var viewModel = new BodyTrendsViewModel(
            weightTrend,
            bodyFatTrend);

        Assert.True(viewModel.HasAnyAvailableTrend);
    }
}