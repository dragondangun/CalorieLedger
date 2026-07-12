using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed record NutritionGoalDraft(
    WeightGoalType GoalType,
    decimal? TargetWeightKg = null,
    decimal? TargetBodyFatPercent = null,
    decimal? TargetMuscleMassKg = null,
    decimal? TargetMusclePercent = null,
    EnergyStrategyMode StrategyMode = EnergyStrategyMode.BalancePercent,
    decimal? StrategyValue = null,
    decimal? StopAtBodyFatPercent = null,
    MassGainIntent? MassGainIntent = null);