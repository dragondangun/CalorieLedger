using CalorieLedger.Application.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CalorieLedger.Persistence;

public sealed class JsonActivityPresetStore:IActivityPresetStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<ActivityPreset>> jsonFile;

    public JsonActivityPresetStore(string filePath) {
        jsonFile = new AtomicJsonFile<List<ActivityPreset>>(filePath, SerializerOptions);
    }

    public static JsonActivityPresetStore CreateDefault() {
        return new(CalorieLedgerDataPaths.ActivityPresetsFilePath);
    }

    public IReadOnlyList<ActivityPreset> GetAll() {
        lock(syncRoot) {
            return [
                .. ReadPresets().OrderBy(preset => preset.Name).ThenBy(preset => preset.Code)
            ];
        }
    }

    public ActivityPreset? Get(string code) {
        lock(syncRoot) {
            return ReadPresets().FirstOrDefault(preset => preset.Code == code);
        }
    }

    public void Save(ActivityPreset preset) {
        ArgumentNullException.ThrowIfNull(preset);

        lock(syncRoot) {
            var presets = ReadPresets();
            var index = presets.FindIndex(existing => existing.Code == preset.Code);

            if(index >= 0) {
                presets[index] = preset;
            }
            else {
                presets.Add(preset);
            }

            jsonFile.Write(presets);
        }
    }

    public bool Delete(string code) {
        lock(syncRoot) {
            var presets = ReadPresets();

            if(presets.RemoveAll(preset => preset.Code == code) == 0) {
                return false;
            }

            jsonFile.Write(presets);
            return true;
        }
    }

    private List<ActivityPreset> ReadPresets() {
        return jsonFile.Read() ?? [];
    }
}
