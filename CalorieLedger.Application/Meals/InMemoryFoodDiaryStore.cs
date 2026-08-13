using CalorieLedger.Domain.Meals;

namespace CalorieLedger.Application.Meals;

public sealed class InMemoryFoodDiaryStore:IFoodDiaryStore {
    private readonly List<MealEntry> meals = [];
    private readonly List<FoodLogEntry> foodEntries = [];
    private readonly HashSet<DateOnly> completedDates = [];

    public IReadOnlyList<MealEntry> GetMeals(
        DateOnly startDate,
        DateOnly endDate
    ) {
        ValidateDateRange(startDate, endDate);

        return [
            .. meals
                .Where(meal => meal.Date >= startDate && meal.Date <= endDate)
                .OrderBy(meal => meal.Date)
                .ThenBy(meal => meal.EatenAt is null)
                .ThenBy(meal => meal.EatenAt)
                .ThenBy(meal => meal.Id),
        ];
    }

    public IReadOnlyList<FoodLogEntry> GetFoodEntries(IReadOnlyCollection<Guid> mealEntryIds) {
        ArgumentNullException.ThrowIfNull(mealEntryIds);

        var mealIdSet = mealEntryIds.ToHashSet();

        return [
            .. foodEntries
                .Where(foodEntry => mealIdSet.Contains(foodEntry.MealEntryId))
                .OrderBy(foodEntry => foodEntry.MealEntryId)
                .ThenBy(foodEntry => foodEntry.Id),
        ];
    }

    public IReadOnlyCollection<DateOnly> GetCompletedDates(
        DateOnly startDate,
        DateOnly endDate
    ) {
        ValidateDateRange(startDate, endDate);

        return [
            .. completedDates
                .Where(
                    date => date >= startDate && date <= endDate
                ).OrderBy(date => date),
        ];
    }

    public void SaveMeal(MealEntry meal) {
        ArgumentNullException.ThrowIfNull(meal);

        var existingIndex = meals.FindIndex(existing => existing.Id == meal.Id);

        if(existingIndex >= 0) {
            meals[existingIndex] = meal;

            return;
        }

        meals.Add(meal);
    }

    public void SaveFoodEntry(FoodLogEntry foodEntry) {
        ArgumentNullException.ThrowIfNull(foodEntry);

        var existingIndex = foodEntries.FindIndex(existing => existing.Id == foodEntry.Id);

        if(existingIndex >= 0) {
            foodEntries[existingIndex] = foodEntry;

            return;
        }

        foodEntries.Add(foodEntry);
    }

    public bool DeleteMeal(Guid mealId) {
        var removed = meals.RemoveAll(meal => meal.Id == mealId) > 0;

        if(!removed) {
            return false;
        }

        foodEntries.RemoveAll(foodEntry => foodEntry.MealEntryId == mealId);

        return true;
    }

    public bool DeleteFoodEntry(
        Guid foodEntryId
    ) {
        return foodEntries.RemoveAll(foodEntry => foodEntry.Id == foodEntryId) > 0;
    }

    public void SetDateComplete(
        DateOnly date,
        bool isComplete
    ) {
        if(isComplete) {
            completedDates.Add(date);

            return;
        }

        completedDates.Remove(date);
    }

    private static void ValidateDateRange(
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
    }
}
