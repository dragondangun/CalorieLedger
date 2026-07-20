using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CalorieLedger.Persistence;

public sealed class JsonBodyMeasurementStore
    :IBodyMeasurementStore {
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new()
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,

                WriteIndented = true
            };

    private readonly object syncRoot = new();
    private readonly string filePath;

    public JsonBodyMeasurementStore(
        string filePath) {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            filePath);

        this.filePath =
            Path.GetFullPath(filePath);
    }

    public static JsonBodyMeasurementStore CreateDefault() {
        var localDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var applicationDirectory = Path.Combine(localDataDirectory, "CalorieLedger");

        var filePath = Path.Combine(applicationDirectory, "body-measurements.json");

        return new JsonBodyMeasurementStore(filePath);
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

            var existingIndex = entries.FindIndex(existing => existing.Id == entry.Id);

            if(existingIndex >= 0) {
                entries[existingIndex] = entry;
            }
            else {
                entries.Add(entry);
            }

            WriteEntries(entries);
        }
    }

    public bool Delete(Guid id) {
        if(id == Guid.Empty) {
            return false;
        }

        lock(syncRoot) {
            var entries = ReadEntries();

            var removed = entries.RemoveAll(entry => entry.Id == id) > 0;

            if(!removed) {
                return false;
            }

            WriteEntries(entries);

            return true;
        }
    }

    private List<BodyMeasurementEntry>
        ReadEntries() {
        if(!File.Exists(filePath)) {
            return [];
        }

        try {
            var json = File.ReadAllText(filePath);

            if(string.IsNullOrWhiteSpace(json)) {
                return [];
            }

            return JsonSerializer.Deserialize<List<BodyMeasurementEntry>>(json, SerializerOptions) ?? [];
        }
        catch(JsonException) {
            PreserveCorruptedFile();

            return [];
        }
    }

    private void WriteEntries(IReadOnlyCollection<BodyMeasurementEntry> entries) {
        var directoryPath = Path.GetDirectoryName(filePath);

        if(!string.IsNullOrWhiteSpace(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(entries, SerializerOptions);

        var temporaryFilePath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        try {
            File.WriteAllText(temporaryFilePath, json);

            File.Move(
                temporaryFilePath,
                filePath,
                overwrite: true);
        }
        finally {
            if(File.Exists(temporaryFilePath)) {
                File.Delete(temporaryFilePath);
            }
        }
    }

    private void PreserveCorruptedFile() {
        if(!File.Exists(filePath)) {
            return;
        }

        var corruptedFilePath = $"{filePath}.corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";

        File.Move(filePath, corruptedFilePath);
    }
}