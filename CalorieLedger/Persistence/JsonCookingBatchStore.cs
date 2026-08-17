using CalorieLedger.Application.Cooking;
using CalorieLedger.Domain.Cooking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonCookingBatchStore:ICookingBatchStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
        },
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<CookingBatch>> jsonFile;

    public JsonCookingBatchStore(string filePath) {
        jsonFile = new AtomicJsonFile<List<CookingBatch>>(
            filePath,
            SerializerOptions
        );
    }

    public static JsonCookingBatchStore CreateDefault() {
        return new JsonCookingBatchStore(
            CalorieLedgerDataPaths.CookingBatchesFilePath
        );
    }

    public IReadOnlyList<CookingBatch> GetAll() {
        lock(syncRoot) {
            return [
                .. ReadBatches()
                    .OrderByDescending(batch => batch.CookedDate)
                    .ThenBy(batch => batch.Id),
            ];
        }
    }

    public CookingBatch? GetBySessionId(Guid sessionId) {
        lock(syncRoot) {
            return ReadBatches().FirstOrDefault(batch => batch.SessionId == sessionId);
        }
    }

    public void Save(CookingBatch batch) {
        ArgumentNullException.ThrowIfNull(batch);

        lock(syncRoot) {
            var batches = ReadBatches();

            var index = batches.FindIndex(existing => existing.Id == batch.Id);

            if(index >= 0) {
                batches[index] = batch;
            }
            else {
                batches.Add(batch);
            }

            jsonFile.Write(batches);
        }
    }

    public bool Delete(Guid id) {
        lock(syncRoot) {
            var batches = ReadBatches();

            var removed = batches.RemoveAll(batch => batch.Id == id) > 0;

            if(!removed) {
                return false;
            }

            jsonFile.Write(batches);

            return true;
        }
    }

    private List<CookingBatch> ReadBatches() {
        return jsonFile.Read() ?? [];
    }
}
