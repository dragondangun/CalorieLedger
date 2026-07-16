namespace CalorieLedger.Domain.Profile;

public sealed record BodyMeasurementEntry(
    Guid Id,
    DateOnly Date,
    decimal WeightKg,
    decimal? BodyFatPercent = null,
    decimal? BoneMassKg = null,
    decimal? MuscleMassKg = null,
    decimal? MusclePercent = null);