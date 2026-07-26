using CalorieLedger.Domain.Profile;

namespace CalorieLedger.ViewModels.Profile;

public sealed record BiologicalSexOptionViewModel(
    BiologicalSex Value,
    string Title
);