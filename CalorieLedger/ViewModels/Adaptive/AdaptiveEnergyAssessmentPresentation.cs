namespace CalorieLedger.ViewModels.Adaptive;

public sealed record AdaptiveEnergyAssessmentPresentation(
    AdaptiveEnergyAssessmentState State,
    string Details,
    string Recommendation = "",
    AdaptiveEnergyStrategySuggestion? SuggestedStrategy = null
);