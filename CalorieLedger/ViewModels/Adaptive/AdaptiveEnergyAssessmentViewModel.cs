using System;
using CommunityToolkit.Mvvm.Input;

namespace CalorieLedger.ViewModels.Adaptive;

public sealed partial class AdaptiveEnergyAssessmentViewModel:ViewModelBase {
    private readonly Action? openGoalEditor;

    public string Title { get; }

    public string Summary { get; }

    public string Details { get; }

    public string Recommendation { get; }

    public AdaptiveEnergyAssessmentState State { get; }

    public bool IsAvailable => State != AdaptiveEnergyAssessmentState.Unavailable;

    public bool IsUnavailable => !IsAvailable;

    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    public bool HasRecommendation => !string.IsNullOrWhiteSpace(Recommendation);

    public bool IsWithinTarget => State == AdaptiveEnergyAssessmentState.WithinTarget;

    public bool IsObservationRequired => State == AdaptiveEnergyAssessmentState.ObservationRequired;

    public bool IsAdjustmentSuggested => State == AdaptiveEnergyAssessmentState.AdjustmentSuggested;

    public bool CanOpenGoalEditor => IsAdjustmentSuggested && openGoalEditor is not null;

    private AdaptiveEnergyAssessmentViewModel(
        string summary,
        string details,
        string recommendation,
        AdaptiveEnergyAssessmentState state,
        Action? openGoalEditor = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        Title = "Адаптивная оценка";

        Summary = summary;
        Details = details;
        Recommendation = recommendation;
        State = state;
        this.openGoalEditor = openGoalEditor;
    }

    public static AdaptiveEnergyAssessmentViewModel CreateUnavailable(string details) {
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        return new AdaptiveEnergyAssessmentViewModel(
            summary: "Пока недостаточно данных",
            details: details,
            recommendation: string.Empty,
            state: AdaptiveEnergyAssessmentState.Unavailable
        );
    }

    public static AdaptiveEnergyAssessmentViewModel CreateWithinTarget(string details) {
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        return new AdaptiveEnergyAssessmentViewModel(
            summary: "Темп соответствует цели",
            details: details,
            recommendation: "Текущую энергетическую стратегию можно сохранить.",
            state: AdaptiveEnergyAssessmentState.WithinTarget
        );
    }

    public static AdaptiveEnergyAssessmentViewModel CreateObservationRequired(
        string details,
        string recommendation) {
        ArgumentException.ThrowIfNullOrWhiteSpace(details);
        ArgumentException.ThrowIfNullOrWhiteSpace(recommendation);

        return new AdaptiveEnergyAssessmentViewModel(
            summary: "Нужно продолжить наблюдение",
            details: details,
            recommendation: recommendation,
            state: AdaptiveEnergyAssessmentState.ObservationRequired
        );
    }

    public static AdaptiveEnergyAssessmentViewModel CreateAdjustmentSuggested(
        string details,
        string recommendation,
        Action? openGoalEditor = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(details);
        ArgumentException.ThrowIfNullOrWhiteSpace(recommendation);

        return new AdaptiveEnergyAssessmentViewModel(
            summary: "Рекомендуется изменить стратегию",
            details: details,
            recommendation: recommendation,
            state: AdaptiveEnergyAssessmentState.AdjustmentSuggested,
            openGoalEditor: openGoalEditor
        );
    }

    [RelayCommand(CanExecute = nameof(CanOpenGoalEditor))]
    private void OpenGoalEditor() {
        openGoalEditor?.Invoke();
    }
}
