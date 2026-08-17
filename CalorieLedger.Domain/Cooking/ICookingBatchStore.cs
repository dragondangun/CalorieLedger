using CalorieLedger.Domain.Cooking;
using System;
using System.Collections.Generic;

namespace CalorieLedger.Application.Cooking;

public interface ICookingBatchStore {
    IReadOnlyList<CookingBatch> GetAll();

    CookingBatch? GetBySessionId(Guid sessionId);

    void Save(CookingBatch batch);

    bool Delete(Guid id);
}
