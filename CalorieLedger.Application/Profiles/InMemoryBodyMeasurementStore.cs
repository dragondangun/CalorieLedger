using System.Linq;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class InMemoryBodyMeasurementStore:IBodyMeasurementStore {
    private readonly List<BodyMeasurementEntry> entries = [];

    public IReadOnlyList<BodyMeasurementEntry> GetAll() {
        return entries
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.Id)
            .ToArray();
    }

    public void Save(BodyMeasurementEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        var existingIndex = entries.FindIndex(existing => existing.Id == entry.Id);

        if(existingIndex >= 0) {
            entries[existingIndex] = entry;
            return;
        }

        entries.Add(entry);
    }

    public bool Delete(Guid id) {
        var existingIndex = entries.FindIndex(entry => entry.Id == id);

        if(existingIndex < 0) {
            return false;
        }

        entries.RemoveAt(existingIndex);

        return true;
    }
}
