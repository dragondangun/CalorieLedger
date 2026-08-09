using CalorieLedger.Application.Profiles;
using System;

namespace CalorieLedger.ViewModels.Adaptive;

public sealed class UnavailableAdaptiveEnergyAssessmentPresentationProvider:IAdaptiveEnergyAssessmentPresentationProvider {
    public AdaptiveEnergyAssessmentPresentation GetCurrent(BodyMeasurementHistorySnapshot measurementSnapshot) {
        ArgumentNullException.ThrowIfNull(measurementSnapshot);
        return new AdaptiveEnergyAssessmentPresentation(
            State: AdaptiveEnergyAssessmentState.Unavailable,
            Details: "Для адаптивной оценки нужен достаточный период измерений и несколько последовательных оценок отклонения от цели."
        );
    }
}
