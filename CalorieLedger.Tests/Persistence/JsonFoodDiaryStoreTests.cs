using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonFoodDiaryStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonFoodDiaryStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );

        filePath = Path.Combine(
            directoryPath,
            "food-diary.json"
        );
    }

    [Fact]
    public void GetMeals_MissingFile_ReturnsEmptyList() {
        var store = new JsonFoodDiaryStore(filePath);

        var entries = store.GetMeals(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31)
        );

        Assert.Empty(entries);
    }

    [Fact]
    public void SaveMealAndFoodEntry_PersistAndCanBeLoadedById() {
        var date = new DateOnly(2026, 8, 15);

        var meal = CreateMeal(date);

        var foodEntry = CreateFoodEntry(meal.Id);

        var firstStore = new JsonFoodDiaryStore(filePath);

        firstStore.SaveMeal(meal);

        firstStore.SaveFoodEntry(foodEntry);

        var secondStore = new JsonFoodDiaryStore(filePath);

        Assert.Equal(
            meal,
            secondStore.GetMeal(meal.Id)
        );

        Assert.Equal(
            foodEntry,
            secondStore.GetFoodEntry(foodEntry.Id)
        );
    }

    [Fact]
    public void Save_ExistingIds_UpdatesPersistedEntries() {
        var date = new DateOnly(2026, 8, 15);

        var meal = CreateMeal(date);

        var foodEntry = CreateFoodEntry(meal.Id);

        var store = new JsonFoodDiaryStore(filePath);

        store.SaveMeal(meal);

        store.SaveFoodEntry(foodEntry);

        store.SaveMeal(
            meal with {
                Name = "Поздний обед",
            }
        );

        store.SaveFoodEntry(
            foodEntry with {
                Name = "Исправленное блюдо",
            }
        );

        var reopenedStore = new JsonFoodDiaryStore(filePath);

        Assert.Equal(
            "Поздний обед",
            reopenedStore.GetMeal(meal.Id)?.Name
        );

        Assert.Equal(
            "Исправленное блюдо",
            reopenedStore.GetFoodEntry(foodEntry.Id)?.Name
        );
    }

    [Fact]
    public void DeleteMeal_RemovesAssociatedFoodAndPersistsDeletion() {
        var date = new DateOnly(2026, 8, 15);

        var meal = CreateMeal(date);

        var foodEntry = CreateFoodEntry(meal.Id);

        var firstStore = new JsonFoodDiaryStore(filePath);

        firstStore.SaveMeal(meal);

        firstStore.SaveFoodEntry(foodEntry);

        var deleted = firstStore.DeleteMeal(meal.Id);

        Assert.True(deleted);

        var secondStore = new JsonFoodDiaryStore(filePath);

        Assert.Null(secondStore.GetMeal(meal.Id));

        Assert.Null(secondStore.GetFoodEntry(foodEntry.Id));
    }

    [Fact]
    public void SetDateComplete_PersistsAndCanBeCleared() {
        var date =new DateOnly(2026, 8, 15);

        var firstStore = new JsonFoodDiaryStore(filePath);

        firstStore.SetDateComplete(date, true);

        var secondStore = new JsonFoodDiaryStore(filePath);

        Assert.Contains(date, secondStore.GetCompletedDates(date, date));

        secondStore.SetDateComplete(date, false);

        var thirdStore = new JsonFoodDiaryStore(filePath);

        Assert.Empty(thirdStore.GetCompletedDates(date, date));
    }

    [Fact]
    public void GetMeals_CorruptedJson_PreservesFileAndReturnsEmpty() {
        Directory.CreateDirectory(directoryPath);

        File.WriteAllText(filePath, "{ invalid json");

        var store = new JsonFoodDiaryStore(filePath);

        var result = store.GetMeals(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31)
        );

        Assert.Empty(result);

        Assert.False(File.Exists(filePath));

        var preservedFiles = Directory.GetFiles(directoryPath, "food-diary.json.corrupt-*");

        Assert.Single(preservedFiles);
    }

    private static MealEntry CreateMeal(DateOnly date) {
        return new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Обед",
            Role: MealGroupRole.Lunch,
            EatenAt: new TimeOnly(14, 0)
        );
    }

    private static FoodLogEntry CreateFoodEntry(Guid mealId) {
        return new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: mealId,
            Name: "Творог",
            Quantity: FoodQuantity.Grams(200m),
            Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Per100Grams,
                    CaloriesKcal: 120m,
                    ProteinG: 17m,
                    FatG: 5m,
                    CarbsG: 3m
                ),
            Source: FoodLogSource.Manual
        );
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
