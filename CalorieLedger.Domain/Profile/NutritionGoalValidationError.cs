namespace CalorieLedger.Domain.Profile;

public enum NutritionGoalValidationError {
    InvalidTargetWeight = 1,
    InvalidTargetBodyFatPercent = 2,
    InvalidTargetMuscleMass = 3,
    InvalidTargetMusclePercent = 4,

    MissingEnergyStrategy = 7,

    MaintenanceRequiresNeutralEnergyBalance = 11,

    InvalidStopBodyFatPercent = 13,
    StopBodyFatOnlyForWeightGain = 14,

    MassGainIntentOnlyForWeightGain = 15,

    InvalidEnergyStrategyValue = 17
}