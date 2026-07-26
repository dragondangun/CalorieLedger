using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonUserNutritionProfileStore:IUserNutritionProfileStore {
    private static readonly JsonSerializerOptions serializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
        },
    };

    private readonly object syncRoot = new();
    private readonly AtomicJsonFile<UserNutritionProfile> jsonFile;
    private readonly IUserNutritionProfileProvider fallbackProfileProvider;

    public JsonUserNutritionProfileStore(
        string filePath,
        IUserNutritionProfileProvider fallbackProfileProvider)
    {
        ArgumentNullException.ThrowIfNull(fallbackProfileProvider);

        jsonFile = new AtomicJsonFile<UserNutritionProfile>(
            filePath,
            serializerOptions
        );

        this.fallbackProfileProvider = fallbackProfileProvider;
    }

    public static JsonUserNutritionProfileStore CreateDefault() {
        return new JsonUserNutritionProfileStore(
            CalorieLedgerDataPaths.UserProfileFilePath,
            new SampleUserNutritionProfileProvider()
        );
    }

    public UserNutritionProfile GetCurrentProfile() {
        lock(syncRoot) {
            return jsonFile.Read() ?? fallbackProfileProvider.GetCurrentProfile();
        }
    }

    public void UpdateGoal(NutritionGoal goal) {
        ArgumentNullException.ThrowIfNull(goal);

        lock(syncRoot) {
            var currentProfile = jsonFile.Read()
                ?? fallbackProfileProvider.GetCurrentProfile();

            var updatedProfile = currentProfile with {
                Goal = goal,
            };

            jsonFile.Write(updatedProfile);
        }
    }
}