namespace CalorieLedger.Domain.Profile;

public sealed record GoalStopEvaluation(
    GoalStopStatus Status,
    decimal? CurrentBodyFatPercent,
    decimal? StopAtBodyFatPercent) {
    public bool ShouldStop =>
        Status == GoalStopStatus.Reached;
}