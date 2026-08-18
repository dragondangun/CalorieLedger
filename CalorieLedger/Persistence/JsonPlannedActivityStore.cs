using CalorieLedger.Application.Activities;
using CalorieLedger.Domain.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CalorieLedger.Persistence;

public sealed class JsonPlannedActivityStore:IPlannedActivityStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<List<PlannedActivity>> jsonFile;

    public JsonPlannedActivityStore(string filePath) {
        jsonFile = new AtomicJsonFile<List<PlannedActivity>>(filePath, SerializerOptions);
    }

    public static JsonPlannedActivityStore CreateDefault() {
        return new(CalorieLedgerDataPaths.PlannedActivitiesFilePath);
    }

    public IReadOnlyList<PlannedActivity> GetAll() {
        lock(syncRoot) {
            return [
                .. ReadActivities()
                    .OrderBy(activity => activity.Date)
                    .ThenBy(activity => activity.PlannedAt)
                    .ThenBy(activity => activity.Name)
            ];
        }
    }

    public IReadOnlyList<PlannedActivity> Get(DateOnly startDate, DateOnly endDate) {
        lock(syncRoot) {
            return [
                .. ReadActivities()
                    .Where(activity => activity.Date >= startDate && activity.Date <= endDate)
                    .OrderBy(activity => activity.Date)
                    .ThenBy(activity => activity.PlannedAt)
            ];
        }
    }

    public PlannedActivity? Get(Guid id) {
        lock(syncRoot) {
            return ReadActivities().FirstOrDefault(activity => activity.Id == id);
        }
    }

    public void Save(PlannedActivity activity) {
        ArgumentNullException.ThrowIfNull(activity);

        lock(syncRoot) {
            var activities = ReadActivities();
            var index = activities.FindIndex(existing => existing.Id == activity.Id);

            if(index >= 0) {
                activities[index] = activity;
            }
            else {
                activities.Add(activity);
            }

            jsonFile.Write(activities);
        }
    }

    public bool Delete(Guid id) {
        lock(syncRoot) {
            var activities = ReadActivities();

            if(activities.RemoveAll(activity => activity.Id == id) == 0) {
                return false;
            }

            jsonFile.Write(activities);
            return true;
        }
    }

    private List<PlannedActivity> ReadActivities() {
        return jsonFile.Read() ?? [];
    }
}
