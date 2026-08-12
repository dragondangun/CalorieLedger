using CalorieLedger.Application.Adaptive;
using CalorieLedger.Application.Nutrition;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Adaptive;
using CalorieLedger.Domain.Nutrition;
using System;

namespace CalorieLedger.ViewModels.Adaptive;

public sealed class AdaptiveEnergyAssessmentPresentationProvider:
    IAdaptiveEnergyAssessmentPresentationProvider,
    IAdaptiveEnergyHistoryResetter
{
    private readonly AdaptiveEnergyAssessmentService assessmentService;
    private readonly IDailyEnergyIntakeHistoryProvider intakeHistoryProvider;
    private readonly BodyMeasurementAwareNutritionProfileProvider profileProvider;

    public AdaptiveEnergyAssessmentPresentationProvider(
        AdaptiveEnergyAssessmentService assessmentService,
        IDailyEnergyIntakeHistoryProvider intakeHistoryProvider,
        BodyMeasurementAwareNutritionProfileProvider profileProvider
    ) {
        ArgumentNullException.ThrowIfNull(assessmentService);
        ArgumentNullException.ThrowIfNull(intakeHistoryProvider);
        ArgumentNullException.ThrowIfNull(profileProvider);

        this.assessmentService = assessmentService;
        this.intakeHistoryProvider = intakeHistoryProvider;
        this.profileProvider = profileProvider;
    }

    public AdaptiveEnergyAssessmentPresentation GetCurrent(BodyMeasurementHistorySnapshot measurementSnapshot) {
        ArgumentNullException.ThrowIfNull(measurementSnapshot);

        var profile = profileProvider.GetProfile(
            measurementSnapshot
        );

        var currentTargetCalories = NutritionTargetCalculator.Calculate(
            profile
        ).CaloriesKcal;

        var windowStartDate = measurementSnapshot.AsOfDate.AddDays(
            -(AdaptivePlanDataQualityEvaluator.DefaultWindowDays - 1)
        );

        var intakeEntries = intakeHistoryProvider.GetEntries(
            windowStartDate,
            measurementSnapshot.AsOfDate
        );

        var result = assessmentService.Evaluate(
            measurementSnapshot,
            intakeEntries,
            profile.Goal,
            currentTargetCalories
        );

        return AdaptiveEnergyAssessmentPresentationFactory.Create(
            result,
            profile.Goal
        );
    }

    public void ResetHistory() {
        assessmentService.ResetHistory();
    }
}
