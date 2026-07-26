using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Persistence;

public sealed class JsonUserNutritionProfileStore:IUserNutritionProfileStore {
    private static readonly JsonSerializerOptions serializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {new JsonStringEnumConverter(),},
    };

    private readonly object syncRoot = new();
    private readonly string filePath;
    private readonly IUserNutritionProfileProvider fallbackProfileProvider;

    public JsonUserNutritionProfileStore(
        string filePath,
        IUserNutritionProfileProvider fallbackProfileProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(fallbackProfileProvider);

        this.filePath = Path.GetFullPath(filePath);
        this.fallbackProfileProvider = fallbackProfileProvider;
    }

    public static JsonUserNutritionProfileStore CreateDefault() {
        var localDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var applicationDirectory = Path.Combine(
            localDataDirectory,
            "CalorieLedger"
        );

        var filePath = Path.Combine(
            applicationDirectory,
            "user-profile.json"
        );

        return new JsonUserNutritionProfileStore(
            filePath,
            new SampleUserNutritionProfileProvider()
        );
    }

    public UserNutritionProfile GetCurrentProfile() {
        lock(syncRoot) {
            return ReadProfile() ?? fallbackProfileProvider.GetCurrentProfile();
        }
    }

    public void UpdateGoal(NutritionGoal goal) {
        ArgumentNullException.ThrowIfNull(goal);

        lock(syncRoot) {
            var currentProfile = ReadProfile() ?? fallbackProfileProvider.GetCurrentProfile();

            var updatedProfile = currentProfile with {
                Goal = goal,
            };

            WriteProfile(updatedProfile);
        }
    }

    private UserNutritionProfile? ReadProfile() {
        if(!File.Exists(filePath)) {
            return null;
        }

        try {
            var json = File.ReadAllText(filePath);

            if(string.IsNullOrWhiteSpace(json)) {
                return null;
            }

            return JsonSerializer.Deserialize<UserNutritionProfile>(
                json,
                serializerOptions
            );
        }
        catch(JsonException) {
            PreserveCorruptedFile();

            return null;
        }
    }

    private void WriteProfile(UserNutritionProfile profile) {
        var directoryPath = Path.GetDirectoryName(filePath);

        if(!string.IsNullOrWhiteSpace(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(
            profile,
            serializerOptions
        );

        var temporaryFilePath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        try {
            File.WriteAllText(
                temporaryFilePath,
                json
            );

            File.Move(
                temporaryFilePath,
                filePath,
                overwrite: true
            );
        }
        finally {
            if(File.Exists(temporaryFilePath)) {
                File.Delete(temporaryFilePath);
            }
        }
    }

    private void PreserveCorruptedFile() {
        if(!File.Exists(filePath)) {
            return;
        }

        var corruptedFilePath =
            filePath
            + ".corrupt-"
            + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
            + "-"
            + Guid.NewGuid().ToString("N");

        File.Move(
            filePath,
            corruptedFilePath
        );
    }
}