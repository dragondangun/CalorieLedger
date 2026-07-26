namespace CalorieLedger.ViewModels.Adaptive;

public sealed class UnavailableAdaptiveEnergyAssessmentPresentationProvider:IAdaptiveEnergyAssessmentPresentationProvider {
    public AdaptiveEnergyAssessmentPresentation GetCurrent() {
        return new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.Unavailable,
            Details: "Для адаптивной оценки нужен достаточный период измерений и несколько последовательных оценок отклонения от цели."
        );
    }
}