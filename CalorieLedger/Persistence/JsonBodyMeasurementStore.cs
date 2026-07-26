using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace CalorieLedger.Persistence;

public sealed class JsonBodyMeasurementStore:IBodyMeasurementStore {
    private static readonly JsonSerializerOptions serializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<BodyMeasurementEntry>> jsonFile;

    public JsonBodyMeasurementStore(string filePath) {
        jsonFile = new AtomicJsonFile<List<BodyMeasurementEntry>>(
            filePath,
            serializerOptions
        );
    }

    public static JsonBodyMeasurementStore CreateDefault() {
        return new JsonBodyMeasurementStore(
            CalorieLedgerDataPaths.BodyMeasurementsFilePath
        );
    }

    public IReadOnlyList<BodyMeasurementEntry> GetAll() {
        lock(syncRoot) {
            return ReadEntries()
                .OrderBy(entry => entry.Date)
                .ThenBy(entry => entry.Id)
                .ToArray();
        }
    }

    public void Save(BodyMeasurementEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        lock(syncRoot) {
            var entries = ReadEntries();

            var existingIndex = entries.FindIndex(
                existing => existing.Id == entry.Id
            );

            if(existingIndex >= 0) {
                entries[existingIndex] = entry;
            }
            else {
                entries.Add(entry);
            }

            jsonFile.Write(entries);
        }
    }

    public bool Delete(Guid id) {
        if(id == Guid.Empty) {
            return false;
        }

        lock(syncRoot) {
            var entries = ReadEntries();

            var removed = entries.RemoveAll(
                entry => entry.Id == id
            ) > 0;

            if(!removed) {
                return false;
            }

            jsonFile.Write(entries);

            return true;
        }
    }

    private List<BodyMeasurementEntry> ReadEntries() {
        return jsonFile.Read() ?? [];
    }
}