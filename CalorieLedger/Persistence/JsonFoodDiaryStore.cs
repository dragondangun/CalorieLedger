using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Meals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonFoodDiaryStore:IFoodDiaryStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
        },
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<FoodDiaryJsonData> jsonFile;

    public JsonFoodDiaryStore(string filePath) {
        jsonFile = new AtomicJsonFile<FoodDiaryJsonData>(
            filePath,
            SerializerOptions
        );
    }

    public static JsonFoodDiaryStore CreateDefault() {
        return new JsonFoodDiaryStore(CalorieLedgerDataPaths.FoodDiaryFilePath);
    }

    public IReadOnlyList<MealEntry> GetMeals(DateOnly startDate, DateOnly endDate) {
        ValidateDateRange(startDate, endDate);

        lock(syncRoot) {
            return [
                .. ReadData()
                    .Meals
                    .Where(meal => meal.Date >= startDate && meal.Date <= endDate)
                    .OrderBy(meal => meal.Date)
                    .ThenBy(meal => meal.EatenAt is null)
                    .ThenBy(meal => meal.EatenAt)
                    .ThenBy(meal => meal.Id),
            ];
        }
    }

    public IReadOnlyList<FoodLogEntry> GetFoodEntries(
        IReadOnlyCollection<Guid> mealEntryIds
    ) {
        ArgumentNullException.ThrowIfNull(mealEntryIds);

        var mealIdSet = mealEntryIds.ToHashSet();

        lock(syncRoot) {
            return [
                .. ReadData()
                    .FoodEntries
                    .Where(foodEntry => mealIdSet.Contains(foodEntry.MealEntryId))
                    .OrderBy(foodEntry => foodEntry.MealEntryId)
                    .ThenBy(foodEntry => foodEntry.Id),
            ];
        }
    }

    public IReadOnlyCollection<DateOnly> GetCompletedDates(DateOnly startDate, DateOnly endDate) {
        ValidateDateRange(startDate, endDate);

        lock(syncRoot) {
            return [
                .. ReadData()
                    .CompletedDates
                    .Where(date => date >= startDate && date <= endDate)
                    .OrderBy(date => date),
            ];
        }
    }

    public void SaveMeal(MealEntry meal) {
        ArgumentNullException.ThrowIfNull(meal);

        lock(syncRoot) {
            var data = ReadData();

            var existingIndex = data.Meals.FindIndex(existing => existing.Id == meal.Id);

            if(existingIndex >= 0) {
                data.Meals[existingIndex] = meal;
            }
            else {
                data.Meals.Add(meal);
            }

            jsonFile.Write(data);
        }
    }

    public void SaveFoodEntry(FoodLogEntry foodEntry) {
        ArgumentNullException.ThrowIfNull(foodEntry);

        lock(syncRoot) {
            var data = ReadData();

            var existingIndex = data.FoodEntries.FindIndex(existing => existing.Id == foodEntry.Id);

            if(existingIndex >= 0) {
                data.FoodEntries[existingIndex] = foodEntry;
            }
            else {
                data.FoodEntries.Add(foodEntry);
            }

            jsonFile.Write(data);
        }
    }

    public bool DeleteMeal(Guid mealId) {
        lock(syncRoot) {
            var data = ReadData();

            var removed = data.Meals.RemoveAll(meal => meal.Id == mealId) > 0;

            if(!removed) {
                return false;
            }

            data.FoodEntries.RemoveAll(foodEntry => foodEntry.MealEntryId == mealId);

            jsonFile.Write(data);

            return true;
        }
    }

    public bool DeleteFoodEntry(Guid foodEntryId) {
        lock(syncRoot) {
            var data = ReadData();

            var removed = data.FoodEntries.RemoveAll(foodEntry => foodEntry.Id == foodEntryId) > 0;

            if(!removed) {
                return false;
            }

            jsonFile.Write(data);

            return true;
        }
    }

    public void SetDateComplete(DateOnly date, bool isComplete) {
        lock(syncRoot) {
            var data = ReadData();

            if(isComplete) {
                if(!data.CompletedDates.Contains(date)) {
                    data.CompletedDates.Add(date);
                }
            }
            else {
                data.CompletedDates.RemoveAll(completedDate => completedDate == date);
            }

            jsonFile.Write(data);
        }
    }

    private FoodDiaryJsonData ReadData() {
        return jsonFile.Read() ?? new FoodDiaryJsonData();
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

    public MealEntry? GetMeal(Guid id) {
        lock(syncRoot) {
            return ReadData().Meals.FirstOrDefault(meal => meal.Id == id);
        }
    }

    public FoodLogEntry? GetFoodEntry(Guid id) {
        lock(syncRoot) {
            return ReadData().FoodEntries.FirstOrDefault(foodEntry => foodEntry.Id == id);
        }
    }
}

internal sealed class FoodDiaryJsonData {
    public List<MealEntry> Meals { get; set; } = [];
    public List<FoodLogEntry> FoodEntries { get; set; } = [];
    public List<DateOnly> CompletedDates { get; set; } = [];
}
