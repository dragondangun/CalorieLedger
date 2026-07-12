namespace CalorieLedger.Domain.Profile;

public sealed record EnergyStrategy(
    EnergyStrategyMode Mode,
    decimal Value) {
    public static EnergyStrategy FromBalancePercent(
        decimal percent) {
        return new EnergyStrategy(
            Mode: EnergyStrategyMode.BalancePercent,
            Value: percent);
    }

    public static EnergyStrategy FromWeightChangePerWeek(
        decimal kilogramsPerWeek) {
        return new EnergyStrategy(
            Mode: EnergyStrategyMode.WeightChangePerWeek,
            Value: kilogramsPerWeek);
    }
}