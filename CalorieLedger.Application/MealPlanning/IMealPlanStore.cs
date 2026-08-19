namespace CalorieLedger.Application.MealPlanning;

public interface IMealPlanStore {
    IReadOnlyList<MealPlanDay> GetAll();
    IReadOnlyList<MealPlanDay> Get(DateOnly startDate, DateOnly endDate);
    void Save(MealPlan plan);
    bool Delete(DateOnly date);
}
