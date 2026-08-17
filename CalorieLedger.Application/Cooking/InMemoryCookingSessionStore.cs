using CalorieLedger.Domain.Cooking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Cooking;

public sealed class InMemoryCookingSessionStore:ICookingSessionStore {
    private readonly List<CookingSessionDraft> sessions = [];

    public IReadOnlyList<CookingSessionDraft> GetAll() {
        return [
            .. sessions
                .OrderBy(session => session.Name)
                .ThenBy(session => session.Id),
        ];
    }

    public CookingSessionDraft? Get(Guid id) {
        return sessions.FirstOrDefault(session => session.Id == id);
    }

    public void Save(CookingSessionDraft session) {
        ArgumentNullException.ThrowIfNull(session);

        var index = sessions.FindIndex(existing => existing.Id == session.Id);

        if(index >= 0) {
            sessions[index] = session;

            return;
        }

        sessions.Add(session);
    }

    public bool Delete(Guid id) {
        return sessions.RemoveAll(session => session.Id == id) > 0;
    }
}
