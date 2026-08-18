namespace CalorieLedger.Domain.Activities;

public static class ActivityEnergyEstimator {
    public static decimal EstimateExtraCalories(
        decimal metValue,
        decimal weightKg,
        TimeSpan duration
    ) {
        if(metValue < 1m) {
            throw new ArgumentOutOfRangeException(nameof(metValue));
        }

        if(weightKg <= 0m) {
            throw new ArgumentOutOfRangeException(nameof(weightKg));
        }

        if(duration <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        var hours = (decimal)duration.TotalHours;
        return Math.Round((metValue - 1m) * weightKg * hours, 0, MidpointRounding.AwayFromZero);
    }
}
