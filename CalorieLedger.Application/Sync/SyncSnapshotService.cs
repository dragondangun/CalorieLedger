using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Fridge;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CalorieLedger.Application.Sync;

public sealed class SyncSnapshotService {
    public const string ProtocolName = "calorieledger.sync_snapshot.v1";

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
        },
    };

    private readonly IFridgeStore fridgeStore;
    private readonly ICookingSessionStore cookingSessionStore;
    private readonly ICookingBatchStore cookingBatchStore;
    private readonly ISyncDeviceIdentityStore deviceIdentityStore;
    private readonly TimeProvider timeProvider;

    public SyncSnapshotService(
        IFridgeStore fridgeStore,
        ICookingSessionStore cookingSessionStore,
        ICookingBatchStore cookingBatchStore,
        ISyncDeviceIdentityStore deviceIdentityStore,
        TimeProvider? timeProvider = null
    ) {
        ArgumentNullException.ThrowIfNull(fridgeStore);
        ArgumentNullException.ThrowIfNull(cookingSessionStore);
        ArgumentNullException.ThrowIfNull(cookingBatchStore);
        ArgumentNullException.ThrowIfNull(deviceIdentityStore);

        this.fridgeStore = fridgeStore;
        this.cookingSessionStore = cookingSessionStore;
        this.cookingBatchStore = cookingBatchStore;
        this.deviceIdentityStore = deviceIdentityStore;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public SyncDeviceIdentity DeviceIdentity => deviceIdentityStore.GetOrCreate();

    public string CreateExport() {
        var snapshot = new SyncSnapshot(
            Protocol: ProtocolName,
            SnapshotId: Guid.NewGuid(),
            SourceDeviceId: DeviceIdentity.Id,
            GeneratedAtUtc: timeProvider.GetUtcNow(),
            FridgeItems: [
                .. fridgeStore.GetAll()
                    .OrderBy(item => item.Id),
            ],
            CookingSessions: [
                .. cookingSessionStore.GetAll()
                    .Where(session => cookingBatchStore.GetBySessionId(session.Id) is null)
                    .OrderBy(session => session.Id),
            ]
        );

        return JsonSerializer.Serialize(snapshot, SerializerOptions);
    }

    public SyncSnapshotParseResult Parse(string? json) {
        if(string.IsNullOrWhiteSpace(json)) {
            return Failure(SyncSnapshotParseError.EmptyInput);
        }

        SyncSnapshot? snapshot;

        try {
            snapshot = JsonSerializer.Deserialize<SyncSnapshot>(
                json,
                SerializerOptions
            );
        }
        catch(JsonException) {
            return Failure(SyncSnapshotParseError.InvalidJson);
        }

        if(snapshot is null) {
            return Failure(SyncSnapshotParseError.InvalidJson);
        }

        var errors = Validate(snapshot);

        return new SyncSnapshotParseResult(
            IsSuccess: errors.Count == 0,
            Snapshot: errors.Count == 0 ? snapshot : null,
            Errors: errors
        );
    }

    public SyncSnapshotPreview Preview(SyncSnapshot snapshot) {
        ArgumentNullException.ThrowIfNull(snapshot);

        var localFridge = fridgeStore.GetAll().ToDictionary(item => item.Id);
        var localCookingSessions = cookingSessionStore.GetAll().ToDictionary(session => session.Id);

        var fridgeAdded = 0;
        var fridgeUpdated = 0;
        var fridgeUnchanged = 0;

        foreach(var incoming in snapshot.FridgeItems) {
            if(!localFridge.TryGetValue(incoming.Id, out var local)) {
                fridgeAdded++;
            }
            else if(AreEquivalent(local, incoming)) {
                fridgeUnchanged++;
            }
            else {
                fridgeUpdated++;
            }
        }

        var cookingSessionsAdded = 0;
        var cookingSessionsUpdated = 0;
        var cookingSessionsUnchanged = 0;
        var completedCookingSessionConflicts = 0;

        foreach(var incoming in snapshot.CookingSessions) {
            if(cookingBatchStore.GetBySessionId(incoming.Id) is not null) {
                completedCookingSessionConflicts++;
                continue;
            }

            if(!localCookingSessions.TryGetValue(incoming.Id, out var local)) {
                cookingSessionsAdded++;
            }
            else if(AreEquivalent(local, incoming)) {
                cookingSessionsUnchanged++;
            }
            else {
                cookingSessionsUpdated++;
            }
        }

        return new SyncSnapshotPreview(
            FridgeAdded: fridgeAdded,
            FridgeUpdated: fridgeUpdated,
            FridgeUnchanged: fridgeUnchanged,
            CookingSessionsAdded: cookingSessionsAdded,
            CookingSessionsUpdated: cookingSessionsUpdated,
            CookingSessionsUnchanged: cookingSessionsUnchanged,
            CompletedCookingSessionConflicts: completedCookingSessionConflicts
        );
    }

    public SyncSnapshotApplyResult Apply(SyncSnapshot snapshot) {
        ArgumentNullException.ThrowIfNull(snapshot);

        if(snapshot.SourceDeviceId == DeviceIdentity.Id) {
            throw new InvalidOperationException("A device cannot apply its own sync snapshot.");
        }

        var preview = Preview(snapshot);

        if(snapshot.FridgeItems.Count > 0) {
            fridgeStore.SaveMany(snapshot.FridgeItems);
        }

        foreach(var session in snapshot.CookingSessions) {
            if(cookingBatchStore.GetBySessionId(session.Id) is not null) {
                continue;
            }

            cookingSessionStore.Save(session);
        }

        return new SyncSnapshotApplyResult(
            FridgeAdded: preview.FridgeAdded,
            FridgeUpdated: preview.FridgeUpdated,
            CookingSessionsAdded: preview.CookingSessionsAdded,
            CookingSessionsUpdated: preview.CookingSessionsUpdated,
            CompletedCookingSessionConflicts: preview.CompletedCookingSessionConflicts
        );
    }

    private IReadOnlyList<SyncSnapshotParseError> Validate(SyncSnapshot snapshot) {
        var errors = new List<SyncSnapshotParseError>();

        if(!string.Equals(
                snapshot.Protocol,
                ProtocolName,
                StringComparison.Ordinal
            )
        ) {
            errors.Add(SyncSnapshotParseError.UnsupportedProtocol);
        }

        if(snapshot.SnapshotId == Guid.Empty) {
            errors.Add(SyncSnapshotParseError.MissingSnapshotId);
        }

        if(snapshot.SourceDeviceId == Guid.Empty) {
            errors.Add(SyncSnapshotParseError.MissingSourceDeviceId);
        }
        else if(snapshot.SourceDeviceId == DeviceIdentity.Id) {
            errors.Add(SyncSnapshotParseError.OwnDeviceSnapshot);
        }

        if(snapshot.FridgeItems is null
            || snapshot.FridgeItems.Any(IsInvalidFridgeItem)
        ) {
            errors.Add(SyncSnapshotParseError.InvalidFridgeItem);
        }
        else if(HasDuplicateIds(snapshot.FridgeItems.Select(item => item.Id))) {
            errors.Add(SyncSnapshotParseError.DuplicateFridgeItem);
        }

        if(snapshot.CookingSessions is null
            || snapshot.CookingSessions.Any(IsInvalidCookingSession)
        ) {
            errors.Add(SyncSnapshotParseError.InvalidCookingSession);
        }
        else if(HasDuplicateIds(snapshot.CookingSessions.Select(session => session.Id))) {
            errors.Add(SyncSnapshotParseError.DuplicateCookingSession);
        }

        return errors;
    }

    private static bool HasDuplicateIds(IEnumerable<Guid> ids) {
        var seen = new HashSet<Guid>();

        foreach(var id in ids) {
            if(!seen.Add(id)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsInvalidFridgeItem(FridgeItem? item) {
        return item is null
            || item.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(item.Name)
            || item.Quantity is null
            || item.Quantity.Value < 0m
            || item.Nutrition is null;
    }

    private static bool IsInvalidCookingSession(CookingSessionDraft? session) {
        return session is null
            || session.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(session.Name)
            || session.OutputWeightG <= 0m
            || session.Ingredients is null
            || session.Ingredients.Count == 0
            || session.Ingredients.Any(ingredient =>
                ingredient is null
                || ingredient.Id == Guid.Empty
                || string.IsNullOrWhiteSpace(ingredient.Name)
                || ingredient.Quantity is null
                || ingredient.Quantity.Value <= 0m
                || ingredient.Nutrition is null
            );
    }

    private static bool AreEquivalent<T>(T first, T second) {
        return string.Equals(
            JsonSerializer.Serialize(first, SerializerOptions),
            JsonSerializer.Serialize(second, SerializerOptions),
            StringComparison.Ordinal
        );
    }

    private static SyncSnapshotParseResult Failure(SyncSnapshotParseError error) {
        return new SyncSnapshotParseResult(
            IsSuccess: false,
            Snapshot: null,
            Errors: [error]
        );
    }
}
