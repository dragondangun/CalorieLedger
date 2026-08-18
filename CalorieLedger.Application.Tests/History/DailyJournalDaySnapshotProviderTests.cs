using CalorieLedger.Application.Activities;
using CalorieLedger.Application.History;
using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Tests.History;

public sealed class DailyJournalDaySnapshotProviderTests {
    [Fact]
    public void GetRange_CombinesFoodDaysAndActivitiesByDate() {
        var startDate = new DateOnly(2026, 8, 17);
        var endDate = startDate.AddDays(1);

        var foodStore = new InMemoryFoodDiaryStore();
        var activityStore = new InMemoryActivityStore();

        activityStore.Save(new ActivityEntry(
            Id: Guid.NewGuid(),
            Date: endDate,
            Name: "HEMA",
            BurnedCaloriesKcal: 350m)
        );

        var provider = new DailyJournalDaySnapshotProvider(
            new FoodDiaryDaySnapshotProvider(foodStore),
            activityStore
        );

        var result = provider.GetRange(startDate, endDate);

        Assert.Equal(2, result.Count);
        Assert.Empty(result[0].Activities);

        var activity = Assert.Single(result[1].Activities);
        Assert.Equal("HEMA", activity.Name);
        Assert.Equal(350m, result[1].ExtraActivityBurnedCaloriesKcal);
        Assert.Equal(-350m, result[1].ActivityAdjustedCaloriesKcal);
    }
}
