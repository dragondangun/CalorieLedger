using CalorieLedger.ViewModels.Common;

namespace CalorieLedger.Tests.ViewModels.Common;

public sealed class RussianDayCountFormatterTests {
    [Theory]
    [InlineData(0, "0 дней")]
    [InlineData(1, "1 день")]
    [InlineData(2, "2 дня")]
    [InlineData(4, "4 дня")]
    [InlineData(5, "5 дней")]
    [InlineData(11, "11 дней")]
    [InlineData(12, "12 дней")]
    [InlineData(14, "14 дней")]
    [InlineData(21, "21 день")]
    [InlineData(22, "22 дня")]
    [InlineData(25, "25 дней")]
    [InlineData(101, "101 день")]
    [InlineData(111, "111 дней")]
    public void Format_ReturnsRussianDayForm(
        int dayCount,
        string expected)
    {
        Assert.Equal(
            expected,
            RussianDayCountFormatter.Format(dayCount)
        );
    }
}