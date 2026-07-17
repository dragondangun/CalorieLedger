namespace CalorieLedger.Application.Profiles;

public sealed record BodyMeasurementDraft(
    Guid Id,
    DateOnly Date,
    decimal? WeightKg = null,
    decimal? BodyFatPercent = null,
    decimal? BoneMassKg = null,
    decimal? MuscleMassKg = null,
    decimal? MusclePercent = null);