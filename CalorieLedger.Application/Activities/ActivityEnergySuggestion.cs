using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public sealed record ActivityEnergySuggestion(
    decimal BurnedCaloriesKcal,
    ActivityEnergyCalculation Calculation
);
