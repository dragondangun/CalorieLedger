using CalorieLedger.Application.Activities;
using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Tests.Activities;

public sealed class RecurringPlannedActivityServiceTests {
    [Fact]
    public void GetOccurrences_WeeklySchedule_ReturnsMatchingWeekday() {
        var store = new InMemoryRecurringPlannedActivityStore();
        var service = new RecurringPlannedActivityService(store);

        service.Save(
            new RecurringPlannedActivityDraft(
                Id: Guid.NewGuid(),
                StartDate: new DateOnly(2026, 8, 18),
                DayOfWeek: DayOfWeek.Thursday,
                IntervalWeeks: 1,
                Name: "HEMA"
            )
        );

        Assert.Empty(service.GetOccurrences(new DateOnly(2026, 8, 19)));

        var occurrence = Assert.Single(
            service.GetOccurrences(new DateOnly(2026, 8, 20))
        );

        Assert.Equal("HEMA", occurrence.Name);

        Assert.Single(
            service.GetOccurrences(new DateOnly(2026, 8, 27))
        );
    }

    [Fact]
    public void GetOccurrences_BiweeklySchedule_SkipsAlternatingWeeks() {
        var store = new InMemoryRecurringPlannedActivityStore();
        var service = new RecurringPlannedActivityService(store);

        service.Save(
            new RecurringPlannedActivityDraft(
                Id: Guid.NewGuid(),
                StartDate: new DateOnly(2026, 8, 18),
                DayOfWeek: DayOfWeek.Thursday,
                IntervalWeeks: 2,
                Name: "HEMA"
            )
        );

        Assert.Single(service.GetOccurrences(new DateOnly(2026, 8, 20)));
        Assert.Empty(service.GetOccurrences(new DateOnly(2026, 8, 27)));
        Assert.Single(service.GetOccurrences(new DateOnly(2026, 9, 3)));
    }

    [Fact]
    public void CompleteOccurrence_HidesOnlyCompletedDate() {
        var store = new InMemoryRecurringPlannedActivityStore();
        var service = new RecurringPlannedActivityService(store);
        var scheduleId = Guid.NewGuid();

        service.Save(
            new RecurringPlannedActivityDraft(
                Id: scheduleId,
                StartDate: new DateOnly(2026, 8, 18),
                DayOfWeek: DayOfWeek.Thursday,
                IntervalWeeks: 1,
                Name: "HEMA"
            )
        );

        var completedDate = new DateOnly(2026, 8, 20);

        service.CompleteOccurrence(
            scheduleId,
            completedDate,
            Guid.NewGuid()
        );

        Assert.Empty(service.GetOccurrences(completedDate));
        Assert.Single(service.GetOccurrences(new DateOnly(2026, 8, 27)));
    }

    [Fact]
    public void SkipOccurrence_HidesOnlySkippedDate() {
        var store = new InMemoryRecurringPlannedActivityStore();
        var service = new RecurringPlannedActivityService(store);
        var scheduleId = Guid.NewGuid();

        service.Save(
            new RecurringPlannedActivityDraft(
                Id: scheduleId,
                StartDate: new DateOnly(2026, 8, 18),
                DayOfWeek: DayOfWeek.Thursday,
                IntervalWeeks: 1,
                Name: "HEMA"
            )
        );

        service.SkipOccurrence(
            scheduleId,
            new DateOnly(2026, 8, 20)
        );

        Assert.Empty(service.GetOccurrences(new DateOnly(2026, 8, 20)));
        Assert.Single(service.GetOccurrences(new DateOnly(2026, 8, 27)));
    }
}
