using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public sealed class InMemoryPlannedActivityStore:IPlannedActivityStore {
    private readonly List<PlannedActivity> activities = [];

    public IReadOnlyList<PlannedActivity> GetAll() {
        return [
            .. activities
                .OrderBy(activity => activity.Date)
                .ThenBy(activity => activity.PlannedAt)
                .ThenBy(activity => activity.Name)
        ];
    }

    public IReadOnlyList<PlannedActivity> Get(DateOnly startDate, DateOnly endDate) {
        return [
            .. GetAll().Where(activity => activity.Date >= startDate && activity.Date <= endDate)
        ];
    }

    public PlannedActivity? Get(Guid id) {
        return activities.FirstOrDefault(activity => activity.Id == id);
    }

    public void Save(PlannedActivity activity) {
        ArgumentNullException.ThrowIfNull(activity);

        var index = activities.FindIndex(existing => existing.Id == activity.Id);

        if(index >= 0) {
            activities[index] = activity;
        }
        else {
            activities.Add(activity);
        }
    }

    public bool Delete(Guid id) {
        return activities.RemoveAll(activity => activity.Id == id) > 0;
    }
}
