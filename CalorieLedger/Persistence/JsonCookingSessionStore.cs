using CalorieLedger.Application.Cooking;
using CalorieLedger.Domain.Cooking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonCookingSessionStore:ICookingSessionStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
        },
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<CookingSessionDraft>> jsonFile;

    public JsonCookingSessionStore(string filePath) {
        jsonFile = new AtomicJsonFile<List<CookingSessionDraft>>(filePath, SerializerOptions);
    }

    public static JsonCookingSessionStore CreateDefault() {
        return new JsonCookingSessionStore(CalorieLedgerDataPaths.CookingSessionsFilePath);
    }

    public IReadOnlyList<CookingSessionDraft> GetAll() {
        lock(syncRoot) {
            return [
                .. ReadSessions()
                    .OrderBy(session => session.Name)
                    .ThenBy(session => session.Id),
            ];
        }
    }

    public CookingSessionDraft? Get(Guid id) {
        lock(syncRoot) {
            return ReadSessions().FirstOrDefault(session => session.Id == id);
        }
    }

    public void Save(CookingSessionDraft session) {
        ArgumentNullException.ThrowIfNull(session);

        lock(syncRoot) {
            var sessions = ReadSessions();

            var index = sessions.FindIndex(existing => existing.Id == session.Id);

            if(index >= 0) {
                sessions[index] = session;
            }
            else {
                sessions.Add(session);
            }

            jsonFile.Write(sessions);
        }
    }

    public bool Delete(Guid id) {
        lock(syncRoot) {
            var sessions = ReadSessions();

            var removed = sessions.RemoveAll(session => session.Id == id) > 0;

            if(!removed) {
                return false;
            }

            jsonFile.Write(sessions);

            return true;
        }
    }

    private List<CookingSessionDraft> ReadSessions() {
        return jsonFile.Read() ?? [];
    }
}
