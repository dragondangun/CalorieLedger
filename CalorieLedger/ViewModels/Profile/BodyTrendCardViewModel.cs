using System;

namespace CalorieLedger.ViewModels.Profile;

public sealed class BodyTrendCardViewModel:ViewModelBase {
    public string Title { get; }

    public string CurrentValueSummary { get; }

    public string ChangeSummary { get; }

    public string RateSummary { get; }

    public string PeriodSummary { get; }

    public string StatusSummary { get; }

    public BodyTrendDirection Direction { get; }

    public bool IsAvailable { get; }

    public bool IsUnavailable => !IsAvailable;

    private BodyTrendCardViewModel(
        string title,
        string currentValueSummary,
        string changeSummary,
        string rateSummary,
        string periodSummary,
        string statusSummary,
        BodyTrendDirection direction,
        bool isAvailable) {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        CurrentValueSummary = currentValueSummary;

        ChangeSummary = changeSummary;
        RateSummary = rateSummary;
        PeriodSummary = periodSummary;
        StatusSummary = statusSummary;
        Direction = direction;
        IsAvailable = isAvailable;
    }

    public static BodyTrendCardViewModel CreateAvailable(
        string title,
        string currentValueSummary,
        string changeSummary,
        string rateSummary,
        string periodSummary,
        BodyTrendDirection direction) {
        return new BodyTrendCardViewModel(
            title: title,
            currentValueSummary: currentValueSummary,
            changeSummary: changeSummary,
            rateSummary: rateSummary,
            periodSummary: periodSummary,
            statusSummary: string.Empty,
            direction: direction,
            isAvailable: true);
    }

    public static BodyTrendCardViewModel CreateUnavailable(
        string title,
        string statusSummary) {
        ArgumentException.ThrowIfNullOrWhiteSpace(statusSummary);

        return new BodyTrendCardViewModel(
            title: title,
            currentValueSummary: string.Empty,
            changeSummary: string.Empty,
            rateSummary: string.Empty,
            periodSummary: string.Empty,
            statusSummary: statusSummary,
            direction: BodyTrendDirection.Unknown,
            isAvailable: false);
    }
}