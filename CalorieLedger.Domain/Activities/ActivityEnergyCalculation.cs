namespace CalorieLedger.Domain.Activities;

public sealed record ActivityEnergyCalculation(
    string PresetCode,
    decimal MetValue,
    decimal WeightKg,
    decimal DurationMinutes
);
