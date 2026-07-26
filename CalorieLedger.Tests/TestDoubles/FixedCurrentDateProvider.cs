using CalorieLedger.Application.Time;

namespace CalorieLedger.Tests.TestDoubles;

internal sealed class FixedCurrentDateProvider:ICurrentDateProvider {
    private readonly DateOnly currentDate;

    public FixedCurrentDateProvider(DateOnly currentDate) {
        this.currentDate = currentDate;
    }

    public DateOnly GetCurrentDate() {
        return currentDate;
    }
}