using CalorieLedger.Application.Activities;
using CalorieLedger.Domain.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CalorieLedger.Persistence;

public sealed class JsonActivityStore:IActivityStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<ActivityEntry>> jsonFile;

    public JsonActivityStore(string filePath) {
        jsonFile = new AtomicJsonFile<List<ActivityEntry>>(
            filePath,
            SerializerOptions
        );
    }

    public static JsonActivityStore CreateDefault() {
        return new JsonActivityStore(CalorieLedgerDataPaths.ActivitiesFilePath);
    }

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

        lock(syncRoot) {
            return [
                .. ReadEntries()
                    .Where(entry => entry.Date >= startDate && entry.Date <= endDate)
                    .OrderBy(entry => entry.Date)
                    .ThenBy(entry => entry.StartedAt is null)
                    .ThenBy(entry => entry.StartedAt)
                    .ThenBy(entry => entry.Name)
                    .ThenBy(entry => entry.Id),
            ];
        }
    }

    public ActivityEntry? Get(Guid id) {
        lock(syncRoot) {
            return ReadEntries().FirstOrDefault(entry => entry.Id == id);
        }
    }

    public void Save(ActivityEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        lock(syncRoot) {
            var entries = ReadEntries();

            var index = entries.FindIndex(existing => existing.Id == entry.Id);

            if(index >= 0) {
                entries[index] = entry;
            }
            else {
                entries.Add(entry);
            }

            jsonFile.Write(entries);
        }
    }

    public bool Delete(Guid id) {
        lock(syncRoot) {
            var entries = ReadEntries();

            var removed = entries.RemoveAll(entry => entry.Id == id) > 0;

            if(!removed) {
                return false;
            }

            jsonFile.Write(entries);

            return true;
        }
    }

    private List<ActivityEntry> ReadEntries() {
        return jsonFile.Read() ?? [];
    }
}
