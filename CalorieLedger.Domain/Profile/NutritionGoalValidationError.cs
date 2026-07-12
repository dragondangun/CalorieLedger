namespace CalorieLedger.Domain.Profile;

public enum NutritionGoalValidationError {
    InvalidTargetWeight = 1,
    InvalidTargetBodyFatPercent = 2,
    InvalidTargetMuscleMass = 3,
    InvalidTargetMusclePercent = 4,

    InvalidDesiredWeightChange = 5,
    InvalidEnergyBalancePercent = 6,

    MissingEnergyStrategy = 7,
    ConflictingEnergyStrategies = 8,

    WeightLossRequiresDeficit = 9,
    WeightGainRequiresSurplus = 10,

    MaintenanceRequiresNeutralEnergyBalance = 11,
    WeightChangeNotAllowedForMaintenance = 12,

    InvalidStopBodyFatPercent = 13,
    StopBodyFatOnlyForWeightGain = 14,

    MassGainIntentOnlyForWeightGain = 15
}