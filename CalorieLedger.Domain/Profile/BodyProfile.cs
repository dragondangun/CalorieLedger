namespace CalorieLedger.Domain.Profile;

public sealed record BodyProfile(
    BiologicalSex Sex,
    int AgeYears,
    decimal HeightCm,
    decimal WeightKg,
    decimal? BodyFatPercent = null,
    decimal? BoneMassKg = null,
    decimal? MuscleMassKg = null,
    decimal? MusclePercent = null);