using System;

namespace CalorieLedger.ViewModels.Adaptive;

public static class AdaptiveEnergyAssessmentViewModelFactory {
    public static AdaptiveEnergyAssessmentViewModel Create(
        AdaptiveEnergyAssessmentPresentation presentation,
        Action openGoalEditor)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(openGoalEditor);

        return presentation.State switch {
            AdaptiveEnergyAssessmentState.Unavailable =>
                AdaptiveEnergyAssessmentViewModel.CreateUnavailable(
                    presentation.Details
                ),

            AdaptiveEnergyAssessmentState.WithinTarget =>
                AdaptiveEnergyAssessmentViewModel.CreateWithinTarget(
                    presentation.Details
                ),

            AdaptiveEnergyAssessmentState.ObservationRequired =>
                AdaptiveEnergyAssessmentViewModel.CreateObservationRequired(
                    details: presentation.Details,
                    recommendation: presentation.Recommendation
                ),

            AdaptiveEnergyAssessmentState.AdjustmentSuggested =>
                AdaptiveEnergyAssessmentViewModel.CreateAdjustmentSuggested(
                    details: presentation.Details,
                    recommendation: presentation.Recommendation,
                    openGoalEditor: openGoalEditor
                ),

            _ => throw new ArgumentOutOfRangeException(
                nameof(presentation),
                presentation.State,
                null
            )
        };
    }
}