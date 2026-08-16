using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonProductCatalogStore:IProductCatalogStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
        },
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<ProductCatalogItem>> jsonFile;

    public JsonProductCatalogStore(string filePath) {
        jsonFile = new AtomicJsonFile<List<ProductCatalogItem>>(
            filePath,
            SerializerOptions
        );
    }

    public static JsonProductCatalogStore CreateDefault() {
        return new JsonProductCatalogStore(CalorieLedgerDataPaths.ProductCatalogFilePath);
    }

    public IReadOnlyList<ProductCatalogItem> GetAll() {
        lock(syncRoot) {
            return [
                .. ReadItems()
                    .OrderBy(item => item.Name)
                    .ThenBy(item => item.Brand)
                    .ThenBy(item => item.Id),
            ];
        }
    }

    public ProductCatalogItem? Get(Guid id) {
        lock(syncRoot) {
            return ReadItems().FirstOrDefault(item => item.Id == id);
        }
    }

    public void Save(ProductCatalogItem item) {
        ArgumentNullException.ThrowIfNull(item);

        lock(syncRoot) {
            var items = ReadItems();

            var existingIndex = items.FindIndex(existing => existing.Id == item.Id);

            if(existingIndex >= 0) {
                items[existingIndex] = item;
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

    private List<ProductCatalogItem> ReadItems() {
        return jsonFile.Read() ?? [];
    }
}
