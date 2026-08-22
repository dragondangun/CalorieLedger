using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Fridge;

namespace CalorieLedger.Application.Sync;

public sealed record SyncSnapshot(
    string Protocol,
    Guid SnapshotId,
    Guid SourceDeviceId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<FridgeItem> FridgeItems,
    IReadOnlyList<CookingSessionDraft> CookingSessions
);
