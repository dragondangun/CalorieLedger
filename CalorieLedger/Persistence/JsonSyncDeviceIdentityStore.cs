using CalorieLedger.Application.Sync;
using System;
using System.Text.Json;

namespace CalorieLedger.Persistence;

public sealed class JsonSyncDeviceIdentityStore:ISyncDeviceIdentityStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<SyncDeviceIdentity> jsonFile;

    public JsonSyncDeviceIdentityStore(string filePath) {
        jsonFile = new AtomicJsonFile<SyncDeviceIdentity>(
            filePath,
            SerializerOptions
        );
    }

    public static JsonSyncDeviceIdentityStore CreateDefault() {
        return new JsonSyncDeviceIdentityStore(
            CalorieLedgerDataPaths.SyncDeviceIdentityFilePath
        );
    }

    public SyncDeviceIdentity GetOrCreate() {
        lock(syncRoot) {
            var existing = jsonFile.Read();

            if(existing is not null && existing.Id != Guid.Empty) {
                return existing;
            }

            var created = new SyncDeviceIdentity(Guid.NewGuid());
            jsonFile.Write(created);

            return created;
        }
    }
}
