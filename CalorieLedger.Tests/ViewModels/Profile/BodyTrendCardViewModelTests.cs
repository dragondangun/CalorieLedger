using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class BodyTrendCardViewModelTests {
    [Fact]
    public void CreateAvailable_CreatesAvailableCard() {
        var viewModel = BodyTrendCardViewModel.CreateAvailable(
            title: "Вес",
            currentValueSummary: "79,5 кг",
            changeSummary: "−0,8 кг",
            rateSummary: "−0,4 кг/нед.",
            periodSummary: "за 14 дней",
            direction: BodyTrendDirection.Decreasing);

        Assert.True(viewModel.IsAvailable);
        Assert.False(viewModel.IsUnavailable);

        Assert.Equal(
            "Вес",
            viewModel.Title);

        Assert.Equal(
            "79,5 кг",
            viewModel.CurrentValueSummary);

        Assert.Equal(
            BodyTrendDirection.Decreasing,
            viewModel.Direction);
    }

    [Fact]
    public void CreateUnavailable_CreatesUnavailableCard() {
        var viewModel = BodyTrendCardViewModel.CreateUnavailable(
            title: "Процент жира",
            statusSummary: "Недостаточно измерений.");

        Assert.False(viewModel.IsAvailable);
        Assert.True(viewModel.IsUnavailable);

        Assert.Equal(
            "Недостаточно измерений.",
            viewModel.StatusSummary);

        Assert.Equal(
            BodyTrendDirection.Unknown,
            viewModel.Direction);
    }
}