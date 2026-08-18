using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public interface IPlannedActivityStore {
    IReadOnlyList<PlannedActivity> GetAll();
    IReadOnlyList<PlannedActivity> Get(DateOnly startDate, DateOnly endDate);
    PlannedActivity? Get(Guid id);
    void Save(PlannedActivity activity);
    bool Delete(Guid id);
}
