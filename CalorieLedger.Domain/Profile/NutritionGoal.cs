namespace CalorieLedger.Domain.Profile;

public sealed record NutritionGoal(
    WeightGoalType GoalType,
    decimal? TargetWeightKg = null,
    decimal? TargetBodyFatPercent = null,
    decimal? TargetMuscleMassKg = null,
    decimal? TargetMusclePercent = null,
    decimal? DesiredWeightChangeKgPerWeek = null,
    decimal? EnergyBalancePercent = null,
    decimal? StopAtBodyFatPercent = null,
    MassGainIntent? MassGainIntent = null);