using CalorieLedger.Application.Profiles;

namespace CalorieLedger.ViewModels.Adaptive;

public interface IAdaptiveEnergyAssessmentPresentationProvider {
    AdaptiveEnergyAssessmentPresentation GetCurrent(BodyMeasurementHistorySnapshot measurementSnapshot);
}
