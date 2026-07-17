using CalorieLedger.Domain.Adaptive;
using System.Collections.Generic;

namespace CalorieLedger.Application.Adaptive;

public interface IAdaptiveEnergyEvaluationStore {
    IReadOnlyList<AdaptiveEnergyEvaluationEntry>
        GetAll();

    void Save(
        AdaptiveEnergyEvaluationEntry entry);

    void Clear();
}