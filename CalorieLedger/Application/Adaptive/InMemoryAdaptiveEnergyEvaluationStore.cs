using CalorieLedger.Domain.Adaptive;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Adaptive;

public sealed class InMemoryAdaptiveEnergyEvaluationStore:IAdaptiveEnergyEvaluationStore {
    private readonly List<AdaptiveEnergyEvaluationEntry> _entries = [];

    public IReadOnlyList<AdaptiveEnergyEvaluationEntry> GetAll() {
        return _entries
            .OrderBy(
                entry =>
                    entry.EvaluationDate)
            .ToArray();
    }

    public void Save(
        AdaptiveEnergyEvaluationEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        var existingIndex =
            _entries.FindIndex(
                existing =>
                    existing.EvaluationDate
                    == entry.EvaluationDate);

        if(existingIndex >= 0) {
            _entries[existingIndex] = entry;
            return;
        }

        _entries.Add(entry);
    }

    public void Clear() {
        _entries.Clear();
    }
}