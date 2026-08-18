using CalorieLedger.Domain.Activities;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonRecurringPlannedActivityStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonRecurringPlannedActivityStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );

        filePath = Path.Combine(
            directoryPath,
            "recurring-planned-activities.json"
        );
    }

    [Fact]
    public void Save_ScheduleAndOccurrenceState_PersistAcrossInstances() {
        var schedule = new RecurringPlannedActivity(
            Id: Guid.NewGuid(),
            StartDate: new DateOnly(2026, 8, 18),
            DayOfWeek: DayOfWeek.Thursday,
            IntervalWeeks: 1,
            Name: "HEMA"
        );

        var date = new DateOnly(2026, 8, 20);
        var firstStore = new JsonRecurringPlannedActivityStore(filePath);

        firstStore.Save(schedule);
        firstStore.SaveOccurrenceState(
            new RecurringPlannedActivityOccurrenceState(
                schedule.Id,
                date,
                RecurringPlannedActivityOccurrenceStatus.Completed,
                Guid.NewGuid()
            )
        );

        var secondStore = new JsonRecurringPlannedActivityStore(filePath);

        Assert.Equal(schedule, secondStore.Get(schedule.Id));
        Assert.NotNull(secondStore.GetOccurrenceState(schedule.Id, date));
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
