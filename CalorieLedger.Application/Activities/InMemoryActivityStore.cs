using CalorieLedger.Domain.Activities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Activities;

public sealed class InMemoryActivityStore:IActivityStore {
    private readonly List<ActivityEntry> entries = [];

    public IReadOnlyList<ActivityEntry> Get(
        DateOnly startDate,
        DateOnly endDate
    ) {
        if(endDate < startDate) {
            throw new ArgumentOutOfRangeException(
                nameof(endDate),
                endDate,
                "End date cannot be earlier than start date."
            );
        }

        return [
            .. entries
                .Where(entry => entry.Date >= startDate && entry.Date <= endDate)
                .OrderBy(entry => entry.Date)
                .ThenBy(entry => entry.StartedAt is null)
                .ThenBy(entry => entry.StartedAt)
                .ThenBy(entry => entry.Name)
                .ThenBy(entry => entry.Id),
        ];
    }

    public ActivityEntry? Get(Guid id) {
        return entries.FirstOrDefault(entry => entry.Id == id);
    }

    public void Save(ActivityEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        var index = entries.FindIndex(existing => existing.Id == entry.Id);

        if(index >= 0) {
            entries[index] = entry;

            return;
        }

        entries.Add(entry);
    }

    public bool Delete(Guid id) {
        return entries.RemoveAll(entry => entry.Id == id) > 0;
    }
}
