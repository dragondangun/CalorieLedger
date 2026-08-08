using System;

namespace CalorieLedger.ViewModels.Adaptive;

public static class AdaptiveEnergyAssessmentViewModelFactory {
    public static AdaptiveEnergyAssessmentViewModel Create(
        AdaptiveEnergyAssessmentPresentation presentation,
        Action<AdaptiveEnergyStrategySuggestion> openGoalEditor
    ) {
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
                CreateAdjustmentSuggested(
                    presentation,
                    openGoalEditor
                ),

            _ => throw new ArgumentOutOfRangeException(
                nameof(presentation),
                presentation.State,
                null
            )
        };
    }

    private static AdaptiveEnergyAssessmentViewModel CreateAdjustmentSuggested(
        AdaptiveEnergyAssessmentPresentation presentation,
        Action<AdaptiveEnergyStrategySuggestion> openGoalEditor) {
        Action? openGoalEditorAction = null;

        if(presentation.SuggestedStrategy is not null) {
            openGoalEditorAction = () =>
                openGoalEditor(
                    presentation.SuggestedStrategy
                );
        }

        return AdaptiveEnergyAssessmentViewModel.CreateAdjustmentSuggested(
            details: presentation.Details,
            recommendation: presentation.Recommendation,
            openGoalEditor: openGoalEditorAction
        );
    }
}
