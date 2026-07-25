using CalorieLedger.Domain.Profile;

namespace CalorieLedger.ViewModels.Adaptive;

public sealed record AdaptiveEnergyStrategySuggestion(
    EnergyStrategyMode Mode,
    decimal Value
);