namespace CalorieLedger.Application.Sync;

public sealed record SyncSnapshotApplyResult(
    int FridgeAdded,
    int FridgeUpdated,
    int CookingSessionsAdded,
    int CookingSessionsUpdated,
    int CompletedCookingSessionConflicts
);
