using CalorieLedger.Domain.Cooking;
using System;
using System.Collections.Generic;

namespace CalorieLedger.Application.Cooking;

public interface ICookingSessionStore {
    IReadOnlyList<CookingSessionDraft> GetAll();

    CookingSessionDraft? Get(Guid id);

    void Save(CookingSessionDraft session);

    bool Delete(Guid id);
}
