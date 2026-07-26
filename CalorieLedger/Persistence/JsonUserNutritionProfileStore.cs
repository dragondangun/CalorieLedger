using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonUserNutritionProfileStore:IUserNutritionProfileStore, IUserNutritionProfileWriter {
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
            return ReadCurrentProfile();
        }
    }

    public void UpdateGoal(NutritionGoal goal) {
        ArgumentNullException.ThrowIfNull(goal);

        lock(syncRoot) {
            var currentProfile = ReadCurrentProfile();

            var updatedProfile = currentProfile with {
                Goal = goal,
            };

            jsonFile.Write(updatedProfile);
        }
    }

    public void UpdateProfile(UserNutritionProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);

        lock(syncRoot) {
            jsonFile.Write(profile);
        }
    }

    private UserNutritionProfile ReadCurrentProfile() {
        return jsonFile.Read() ?? fallbackProfileProvider.GetCurrentProfile();
    }
}