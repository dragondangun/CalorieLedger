using CalorieLedger.Application.Activities;
using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Tests.Activities;

public sealed class PlannedActivityServiceTests {
    [Fact]
    public void Get_Date_ReturnsOnlyPlansForRequestedDay() {
        var date = new DateOnly(2026, 8, 18);
        var store = new InMemoryPlannedActivityStore();
        var service = new PlannedActivityService(store);

        store.Save(
            new PlannedActivity(
                Guid.NewGuid(),
                date.AddDays(-1),
                "Вчера"
            )
        );

        store.Save(
            new PlannedActivity(
                Guid.NewGuid(),
                date,
                "Сегодня 2",
                new TimeOnly(20, 0)
            )
        );

        store.Save(
            new PlannedActivity(
                Guid.NewGuid(),
                date,
                "Сегодня 1",
                new TimeOnly(18, 0)
            )
        );

        store.Save(
            new PlannedActivity(
                Guid.NewGuid(),
                date.AddDays(1),
                "Завтра"
            )
        );

        var result = service.Get(date);

        Assert.Equal(2, result.Count);
        Assert.Equal("Сегодня 1", result[0].Name);
        Assert.Equal("Сегодня 2", result[1].Name);
    }

    [Fact]
    public void Get_Range_ReturnsPlansInsideRange() {
        var startDate = new DateOnly(2026, 8, 18);
        var endDate = new DateOnly(2026, 8, 20);
        var store = new InMemoryPlannedActivityStore();
        var service = new PlannedActivityService(store);

        store.Save(new PlannedActivity(Guid.NewGuid(), startDate.AddDays(-1), "До"));
        store.Save(new PlannedActivity(Guid.NewGuid(), startDate, "Начало"));
        store.Save(new PlannedActivity(Guid.NewGuid(), endDate, "Конец"));
        store.Save(new PlannedActivity(Guid.NewGuid(), endDate.AddDays(1), "После"));

        var result = service.Get(startDate, endDate);

        Assert.Equal(2, result.Count);
        Assert.Equal("Начало", result[0].Name);
        Assert.Equal("Конец", result[1].Name);
    }
}
