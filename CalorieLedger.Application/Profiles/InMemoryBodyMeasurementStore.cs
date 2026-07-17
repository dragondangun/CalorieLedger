using System.Linq;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class InMemoryBodyMeasurementStore
    :IBodyMeasurementStore {
    private readonly List<BodyMeasurementEntry>
        _entries = [];

    public IReadOnlyList<BodyMeasurementEntry> GetAll() {
        return _entries
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.Id)
            .ToArray();
    }

    public void Save(
        BodyMeasurementEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        var existingIndex =
            _entries.FindIndex(
                existing =>
                    existing.Id == entry.Id);

        if(existingIndex >= 0) {
            _entries[existingIndex] = entry;
            return;
        }

        _entries.Add(entry);
    }

    public bool Delete(
        Guid id) {
        var existingIndex =
            _entries.FindIndex(
                entry =>
                    entry.Id == id);

        if(existingIndex < 0) {
            return false;
        }

        _entries.RemoveAt(existingIndex);

        return true;
    }
}