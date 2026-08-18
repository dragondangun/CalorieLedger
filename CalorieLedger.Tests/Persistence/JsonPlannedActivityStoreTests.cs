using CalorieLedger.Domain.Activities;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonPlannedActivityStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonPlannedActivityStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );
        filePath = Path.Combine(directoryPath, "planned-activities.json");
    }

    [Fact]
    public void Save_Plan_PersistsAcrossStoreInstances() {
        var plan = new PlannedActivity(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 20),
            Name: "HEMA",
            PlannedAt: new TimeOnly(19, 0),
            Duration: TimeSpan.FromMinutes(90),
            PresetCode: "custom:hema",
            MetValue: 7m,
            Note: "Тренировка"
        );

        new JsonPlannedActivityStore(filePath).Save(plan);

        var saved = new JsonPlannedActivityStore(filePath).Get(plan.Id);

        Assert.Equal(plan, saved);
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
