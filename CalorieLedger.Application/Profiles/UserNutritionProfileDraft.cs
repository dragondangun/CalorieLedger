using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed record UserNutritionProfileDraft(
    Guid Id,
    string DisplayName,
    BiologicalSex Sex,
    int? AgeYears,
    decimal? HeightCm,
    LifestyleActivityLevel LifestyleActivityLevel
);