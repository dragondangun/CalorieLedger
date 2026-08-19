namespace CalorieLedger.Application.MealPlanning;

public sealed class InMemoryMealPlanStore:IMealPlanStore {
    private readonly List<MealPlanDay> days = [];

    public IReadOnlyList<MealPlanDay> GetAll() {
        return [
            .. days.OrderBy(day => day.Date),
        ];
    }

    public IReadOnlyList<MealPlanDay> Get(DateOnly startDate, DateOnly endDate) {
        ValidateDateRange(startDate, endDate);

        return [
            .. days
                .Where(day => day.Date >= startDate && day.Date <= endDate)
                .OrderBy(day => day.Date),
        ];
    }

    public void Save(MealPlan plan) {
        ArgumentNullException.ThrowIfNull(plan);

        if(plan.Days.Count == 0) {
            return;
        }

        var replacedDates = plan.Days
            .Select(day => day.Date)
            .ToHashSet();

        days.RemoveAll(day => replacedDates.Contains(day.Date));
        days.AddRange(plan.Days);
    }

    public bool Delete(DateOnly date) {
        return days.RemoveAll(day => day.Date == date) > 0;
    }

    private static void ValidateDateRange(DateOnly startDate, DateOnly endDate) {
        if(endDate < startDate) {
            throw new ArgumentOutOfRangeException(
                nameof(endDate),
                endDate,
                "End date cannot be earlier than start date."
            );
        }
    }
}
