using CalorieLedger.Domain.Meals;

namespace CalorieLedger.Application.Meals;

public interface IFoodDiaryStore {
    IReadOnlyList<MealEntry> GetMeals(
        DateOnly startDate,
        DateOnly endDate
    );

    IReadOnlyList<FoodLogEntry> GetFoodEntries(IReadOnlyCollection<Guid> mealEntryIds);

    IReadOnlyCollection<DateOnly> GetCompletedDates(
        DateOnly startDate,
        DateOnly endDate
    );

    void SaveMeal(MealEntry meal);

    void SaveFoodEntry(FoodLogEntry foodEntry);

    bool DeleteMeal(Guid mealId);

    bool DeleteFoodEntry(Guid foodEntryId);

    void SetDateComplete(DateOnly date, bool isComplete);
}
