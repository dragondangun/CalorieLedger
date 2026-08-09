using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Nutrition;

public interface IDailyEnergyIntakeHistoryProvider {
    IReadOnlyList<DailyEnergyIntakeEntry> GetEntries(
        DateOnly startDate,
        DateOnly endDate
    );
}
