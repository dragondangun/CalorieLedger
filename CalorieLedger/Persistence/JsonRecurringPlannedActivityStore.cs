using CalorieLedger.Application.Activities;
using CalorieLedger.Domain.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CalorieLedger.Persistence;

public sealed class JsonRecurringPlannedActivityStore:IRecurringPlannedActivityStore {
    private sealed record Document(
        List<RecurringPlannedActivity> Schedules,
        List<RecurringPlannedActivityOccurrenceState> States
    );

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<Document> jsonFile;

    public JsonRecurringPlannedActivityStore(string filePath) {
        jsonFile = new AtomicJsonFile<Document>(filePath, SerializerOptions);
    }

    public static JsonRecurringPlannedActivityStore CreateDefault() {
        return new(CalorieLedgerDataPaths.RecurringPlannedActivitiesFilePath);
    }

    public IReadOnlyList<RecurringPlannedActivity> GetAll() {
        lock(syncRoot) {
            return [
                .. ReadDocument().Schedules
                    .OrderBy(schedule => schedule.DayOfWeek)
                    .ThenBy(schedule => schedule.PlannedAt)
                    .ThenBy(schedule => schedule.Name)
            ];
        }
    }

    public RecurringPlannedActivity? Get(Guid id) {
        lock(syncRoot) {
            return ReadDocument().Schedules.FirstOrDefault(schedule => schedule.Id == id);
        }
    }

    public void Save(RecurringPlannedActivity schedule) {
        ArgumentNullException.ThrowIfNull(schedule);

        lock(syncRoot) {
            var document = ReadDocument();
            var index = document.Schedules.FindIndex(existing => existing.Id == schedule.Id);

            if(index >= 0) {
                document.Schedules[index] = schedule;
            }
            else {
                document.Schedules.Add(schedule);
            }

            jsonFile.Write(document);
        }
    }

    public bool Delete(Guid id) {
        lock(syncRoot) {
            var document = ReadDocument();

            if(document.Schedules.RemoveAll(schedule => schedule.Id == id) == 0) {
                return false;
            }

            document.States.RemoveAll(state => state.ScheduleId == id);
            jsonFile.Write(document);
            return true;
        }
    }

    public RecurringPlannedActivityOccurrenceState? GetOccurrenceState(
        Guid scheduleId,
        DateOnly date
    ) {
        lock(syncRoot) {
            return ReadDocument().States.FirstOrDefault(
                state => state.ScheduleId == scheduleId && state.Date == date
            );
        }
    }

    public void SaveOccurrenceState(RecurringPlannedActivityOccurrenceState state) {
        ArgumentNullException.ThrowIfNull(state);

        lock(syncRoot) {
            var document = ReadDocument();

            var index = document.States.FindIndex(
                existing =>
                    existing.ScheduleId == state.ScheduleId
                    && existing.Date == state.Date
            );

            if(index >= 0) {
                document.States[index] = state;
            }
            else {
                document.States.Add(state);
            }

            jsonFile.Write(document);
        }
    }

    private Document ReadDocument() {
        return jsonFile.Read() ?? new([], []);
    }
}
