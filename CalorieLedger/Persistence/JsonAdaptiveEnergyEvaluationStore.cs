using CalorieLedger.Application.Adaptive;
using CalorieLedger.Domain.Adaptive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonAdaptiveEnergyEvaluationStore:IAdaptiveEnergyEvaluationStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
        },
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<AdaptiveEnergyEvaluationEntry>> jsonFile;

    public JsonAdaptiveEnergyEvaluationStore(
        string filePath
    ) {
        jsonFile = new AtomicJsonFile<List<AdaptiveEnergyEvaluationEntry>>(
            filePath,
            SerializerOptions
        );
    }

    public static JsonAdaptiveEnergyEvaluationStore CreateDefault() {
        return new JsonAdaptiveEnergyEvaluationStore(
            CalorieLedgerDataPaths.AdaptiveEnergyEvaluationsFilePath
        );
    }

    public IReadOnlyList<AdaptiveEnergyEvaluationEntry> GetAll() {
        lock(syncRoot) {
            return ReadEntries().OrderBy(entry => entry.EvaluationDate).ToArray();
        }
    }

    public void Save(
        AdaptiveEnergyEvaluationEntry entry
    ) {
        ArgumentNullException.ThrowIfNull(entry);

        lock(syncRoot) {
            var entries = ReadEntries();

            var existingIndex = entries.FindIndex(
                existing => existing.EvaluationDate == entry.EvaluationDate
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

    public void Clear() {
        lock(syncRoot) {
            jsonFile.Write([]);
        }
    }

    private List<AdaptiveEnergyEvaluationEntry> ReadEntries() {
        return jsonFile.Read() ?? [];
    }
}
