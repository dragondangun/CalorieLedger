using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed record NutritionGoalDraft(
    WeightGoalType GoalType,
    decimal? TargetWeightKg = null,
    decimal? TargetBodyFatPercent = null,
    decimal? TargetMuscleMassKg = null,
    decimal? TargetMusclePercent = null,
    decimal? DesiredWeightChangeKgPerWeek = null,
    decimal? EnergyBalancePercent = null,
    decimal? StopAtBodyFatPercent = null,
    MassGainIntent? MassGainIntent = null);