namespace CalorieLedger.Domain.Profile;

public sealed record UserNutritionProfile(
    Guid Id,
    string DisplayName,
    BodyProfile Body,
    LifestyleActivityLevel LifestyleActivityLevel,
    NutritionGoal Goal);