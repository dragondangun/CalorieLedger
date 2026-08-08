using System;

namespace CalorieLedger.ViewModels.Profile;

public static class BodyMeasurementFreshnessPolicy {
    public const int WarningDayCount = 14;

    public static int GetAgeInDays(
        DateOnly measurementDate,
        DateOnly currentDate) {
        return currentDate.DayNumber - measurementDate.DayNumber;
    }

    public static bool IsStale(
        DateOnly measurementDate,
        DateOnly currentDate) {
        return GetAgeInDays(measurementDate, currentDate) > WarningDayCount;
    }
}