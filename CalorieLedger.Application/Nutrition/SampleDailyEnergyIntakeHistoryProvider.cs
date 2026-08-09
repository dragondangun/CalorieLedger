using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Nutrition;

public sealed class SampleDailyEnergyIntakeHistoryProvider:IDailyEnergyIntakeHistoryProvider {
    public IReadOnlyList<DailyEnergyIntakeEntry> GetEntries(
        DateOnly startDate,
        DateOnly endDate
    ) {
        if(endDate < startDate) {
            throw new ArgumentOutOfRangeException(
                nameof(endDate),
                endDate,
                "End date cannot be earlier than start date."
            );
        }

        var dayCount = endDate.DayNumber - startDate.DayNumber + 1;

        return [
            .. Enumerable.Range(0, dayCount).Select(
                day => new DailyEnergyIntakeEntry(
                    Date: startDate.AddDays(day),
                    CaloriesKcal: GetCalories(day),
                    IsComplete: true
                )
            ),
        ];
    }

    private static decimal GetCalories(int dayIndex) {
        var offset = dayIndex % 7;

        return offset switch {
            0 => 2050m,
            1 => 2180m,
            2 => 2350m,
            3 => 1980m,
            4 => 2250m,
            5 => 2100m,
            6 => 2150m,
            _ => throw new ArgumentOutOfRangeException(
                nameof(dayIndex)
            )
        };
    }
}
