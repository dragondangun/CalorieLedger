using System;
using System.IO;
using System.Text.Json;

namespace CalorieLedger.Persistence;

internal sealed class AtomicJsonFile<T> where T : class {
    private readonly string filePath;
    private readonly JsonSerializerOptions serializerOptions;

    public AtomicJsonFile(
        string filePath,
        JsonSerializerOptions serializerOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(serializerOptions);

        this.filePath = Path.GetFullPath(filePath);
        this.serializerOptions = serializerOptions;
    }

    public T? Read() {
        if(!File.Exists(filePath)) {
            return null;
        }

        try {
            var json = File.ReadAllText(filePath);

            if(string.IsNullOrWhiteSpace(json)) {
                return null;
            }

            return JsonSerializer.Deserialize<T>(
                json,
                serializerOptions
            );
        }
        catch(JsonException) {
            PreserveCorruptedFile();

            return null;
        }
    }

    public void Write(T value) {
        ArgumentNullException.ThrowIfNull(value);

        var directoryPath = Path.GetDirectoryName(filePath);

        if(!string.IsNullOrWhiteSpace(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(
            value,
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

        var corruptedFilePath = $"{filePath}.corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";

        File.Move(
            filePath,
            corruptedFilePath
        );
    }
}