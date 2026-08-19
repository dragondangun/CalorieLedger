namespace CalorieLedger.Application.MealPlanning;

public sealed class MealPlanService {
    private readonly IMealPlanStore store;

    public MealPlanService(IMealPlanStore store) {
        ArgumentNullException.ThrowIfNull(store);

        this.store = store;
    }

    public IReadOnlyList<MealPlanDay> GetAll() {
        return store.GetAll();
    }

    public IReadOnlyList<MealPlanDay> Get(DateOnly startDate, DateOnly endDate) {
        return store.Get(startDate, endDate);
    }

    public void Save(MealPlan plan) {
        ArgumentNullException.ThrowIfNull(plan);

        if(plan.Days.Count == 0) {
            throw new ArgumentException(
                "Meal plan must contain at least one day.",
                nameof(plan)
            );
        }

        if(plan.Days.Select(day => day.Date).Distinct().Count() != plan.Days.Count) {
            throw new ArgumentException(
                "Meal plan cannot contain duplicate dates.",
                nameof(plan)
            );
        }

        store.Save(
            plan with {
                Days = [
                    .. plan.Days.OrderBy(day => day.Date),
                ],
            }
        );
    }

    public bool Delete(DateOnly date) {
        return store.Delete(date);
    }
}
