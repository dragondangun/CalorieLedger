using CalorieLedger.Domain.Profile;

namespace CalorieLedger.ViewModels.Profile;

public sealed record LifestyleActivityLevelOptionViewModel(
    LifestyleActivityLevel Value,
    string Title,
    string Description
);