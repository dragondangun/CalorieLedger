using CalorieLedger.Application.Fridge;
using CalorieLedger.Domain.Fridge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonFridgeStore:IFridgeStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
        },
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<FridgeItem>> jsonFile;

    public JsonFridgeStore(string filePath) {
        jsonFile = new AtomicJsonFile<List<FridgeItem>>(
            filePath,
            SerializerOptions
        );
    }

    public static JsonFridgeStore CreateDefault() {
        return new JsonFridgeStore(CalorieLedgerDataPaths.FridgeFilePath);
    }

    public IReadOnlyList<FridgeItem> GetAll() {
        lock(syncRoot) {
            return [
                .. ReadItems()
                    .OrderBy(item => item.ExpirationDate is null)
                    .ThenBy(item => item.ExpirationDate)
                    .ThenBy(item => item.Name)
                    .ThenBy(item => item.Id),
            ];
        }
    }

    public FridgeItem? Get(Guid id) {
        lock(syncRoot) {
            return ReadItems().FirstOrDefault(item => item.Id == id);
        }
    }

    public void Save(FridgeItem item) {
        ArgumentNullException.ThrowIfNull(item);

        lock(syncRoot) {
            var items = ReadItems();

            var index = items.FindIndex(existing => existing.Id == item.Id);

            if(index >= 0) {
                items[index] = item;
            }
            else {
                items.Add(item);
            }

            jsonFile.Write(items);
        }
    }

    public bool Delete(Guid id) {
        lock(syncRoot) {
            var items = ReadItems();

            var removed = items.RemoveAll(item => item.Id == id) > 0;

            if(!removed) {
                return false;
            }

            jsonFile.Write(items);

            return true;
        }
    }

    private List<FridgeItem> ReadItems() {
        return jsonFile.Read() ?? [];
    }
}
