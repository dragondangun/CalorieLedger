using System;

namespace CalorieLedger.ViewModels.Profile;

public static class BodyMeasurementFreshnessPolicy {
    public const int WarningDayCount = 14;

    public static int GetAgeInDays(DateOnly measurementDate, DateOnly currentDate) {
        return currentDate.DayNumber - measurementDate.DayNumber;
    }

    public static bool IsStale(DateOnly measurementDate, DateOnly currentDate) {
        return GetState(measurementDate, currentDate) == BodyMeasurementFreshnessState.Stale;
    }

    public static BodyMeasurementFreshnessState GetState(
        DateOnly measurementDate,
        DateOnly currentDate
    ) {
        var ageInDays = GetAgeInDays(
            measurementDate,
            currentDate
        );

        if(ageInDays < 0) {
            return BodyMeasurementFreshnessState.Future;
        }

        return ageInDays > WarningDayCount
            ? BodyMeasurementFreshnessState.Stale
            : BodyMeasurementFreshnessState.Fresh;
    }
}
