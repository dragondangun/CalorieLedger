using CalorieLedger.Application.Activities;
using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Tests.Activities;

public sealed class RecentActivityServiceTests {
    [Fact]
    public void GetRecent_ReturnsNewestActivitiesWithinLookbackAndLimit() {
        var targetDate = new DateOnly(2026, 8, 18);
        var store = new InMemoryActivityStore();

        Save(store, targetDate.AddDays(-100), "Слишком старая");
        Save(store, targetDate.AddDays(-3), "Третья");
        Save(store, targetDate.AddDays(-1), "Вторая");
        Save(store, targetDate, "Первая");

        var service = new RecentActivityService(store);
        var result = service.GetRecent(targetDate, maxCount: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("Первая", result[0].Name);
        Assert.Equal("Вторая", result[1].Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetRecent_InvalidMaxCount_Throws(int maxCount) {
        var service = new RecentActivityService(new InMemoryActivityStore());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.GetRecent(new DateOnly(2026, 8, 18), maxCount)
        );
    }

    private static void Save(
        IActivityStore store,
        DateOnly date,
        string name
    ) {
        store.Save(
            new ActivityEntry(
                Id: Guid.NewGuid(),
                Date: date,
                Name: name,
                BurnedCaloriesKcal: 100m
            )
        );
    }
}
