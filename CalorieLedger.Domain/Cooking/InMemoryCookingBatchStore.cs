using CalorieLedger.Domain.Cooking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Cooking;

public sealed class InMemoryCookingBatchStore:ICookingBatchStore {
    private readonly List<CookingBatch> batches = [];

    public IReadOnlyList<CookingBatch> GetAll() {
        return [
            .. batches
                .OrderByDescending(batch => batch.CookedDate)
                .ThenBy(batch => batch.Id),
        ];
    }

    public CookingBatch? GetBySessionId(Guid sessionId) {
        return batches.FirstOrDefault(batch => batch.SessionId == sessionId);
    }

    public void Save(CookingBatch batch) {
        ArgumentNullException.ThrowIfNull(batch);

        var index = batches.FindIndex(existing => existing.Id == batch.Id);

        if(index >= 0) {
            batches[index] = batch;

            return;
        }

        batches.Add(batch);
    }

    public bool Delete(Guid id) {
        return batches.RemoveAll(batch => batch.Id == id) > 0;
    }
}
