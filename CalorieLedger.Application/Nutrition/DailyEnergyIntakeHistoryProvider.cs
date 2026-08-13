using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Nutrition;

public sealed class DailyEnergyIntakeHistoryProvider:IDailyEnergyIntakeHistoryProvider {
    private readonly IFoodDiaryStore foodDiaryStore;

    public DailyEnergyIntakeHistoryProvider(IFoodDiaryStore foodDiaryStore) {
        ArgumentNullException.ThrowIfNull(foodDiaryStore);

        this.foodDiaryStore = foodDiaryStore;
    }

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

        var meals = foodDiaryStore.GetMeals(startDate, endDate);

        var mealsById = meals.ToDictionary(meal => meal.Id);

        var foodEntries = foodDiaryStore.GetFoodEntries([.. meals.Select(meal => meal.Id),]);

        var completedDates = foodDiaryStore.GetCompletedDates(startDate, endDate).ToHashSet();

        var caloriesByDate = new Dictionary<DateOnly, decimal>();

        var datesWithUnknownCalories = new HashSet<DateOnly>();

        foreach(var foodEntry in foodEntries) {
            if(!mealsById.TryGetValue(
                foodEntry.MealEntryId,
                out var meal
            )) {
                continue;
            }

            var totals = NutritionCalculator.CalculateTotal(
                foodEntry.Nutrition,
                foodEntry.Quantity
            );

            if(totals.CaloriesKcal is not decimal caloriesKcal) {
                datesWithUnknownCalories.Add(meal.Date);

                continue;
            }

            caloriesByDate.TryGetValue(
                meal.Date,
                out var currentCalories
            );

            caloriesByDate[meal.Date] = currentCalories + caloriesKcal;
        }

        var dayCount = endDate.DayNumber - startDate.DayNumber + 1;

        return [
            .. Enumerable.Range(0, dayCount).Select(
                day => {
                    var date = startDate.AddDays(day);

                    caloriesByDate.TryGetValue(date, out var caloriesKcal);

                    return new DailyEnergyIntakeEntry(
                        Date: date,
                        CaloriesKcal: caloriesKcal,
                        IsComplete: completedDates.Contains(date) && !datesWithUnknownCalories.Contains(date)
                    );
                }
            ),
        ];
    }
}
