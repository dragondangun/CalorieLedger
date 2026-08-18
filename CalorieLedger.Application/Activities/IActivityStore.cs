using CalorieLedger.Domain.Activities;
using System;
using System.Collections.Generic;

namespace CalorieLedger.Application.Activities;

public interface IActivityStore {
    IReadOnlyList<ActivityEntry> Get(
        DateOnly startDate,
        DateOnly endDate
    );

    ActivityEntry? Get(Guid id);

    void Save(ActivityEntry entry);

    bool Delete(Guid id);
}
