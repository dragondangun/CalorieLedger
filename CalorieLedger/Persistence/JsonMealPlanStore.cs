using CalorieLedger.Application.MealPlanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonMealPlanStore:IMealPlanStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
        },
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<MealPlanDay>> jsonFile;

    public JsonMealPlanStore(string filePath) {
        jsonFile = new AtomicJsonFile<List<MealPlanDay>>(
            filePath,
            SerializerOptions
        );
    }

    public static JsonMealPlanStore CreateDefault() {
        return new JsonMealPlanStore(CalorieLedgerDataPaths.MealPlansFilePath);
    }

    public IReadOnlyList<MealPlanDay> GetAll() {
        lock(syncRoot) {
            return [
                .. ReadDays().OrderBy(day => day.Date),
            ];
        }
    }

    public IReadOnlyList<MealPlanDay> Get(DateOnly startDate, DateOnly endDate) {
        ValidateDateRange(startDate, endDate);

        lock(syncRoot) {
            return [
                .. ReadDays()
                    .Where(day => day.Date >= startDate && day.Date <= endDate)
                    .OrderBy(day => day.Date),
            ];
        }
    }

    public void Save(MealPlan plan) {
        ArgumentNullException.ThrowIfNull(plan);

        if(plan.Days.Count == 0) {
            return;
        }

        lock(syncRoot) {
            var days = ReadDays();
            var replacedDates = plan.Days
                .Select(day => day.Date)
                .ToHashSet();

            days.RemoveAll(day => replacedDates.Contains(day.Date));
            days.AddRange(plan.Days);
            days.Sort((left, right) => left.Date.CompareTo(right.Date));

            jsonFile.Write(days);
        }
    }

    public bool Delete(DateOnly date) {
        lock(syncRoot) {
            var days = ReadDays();

            if(days.RemoveAll(day => day.Date == date) == 0) {
                return false;
            }

            jsonFile.Write(days);
            return true;
        }
    }

    private List<MealPlanDay> ReadDays() {
        return jsonFile.Read() ?? [];
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
