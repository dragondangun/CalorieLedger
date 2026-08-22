namespace CalorieLedger.Application.Sync;

public sealed record SyncSnapshotPreview(
    int FridgeAdded,
    int FridgeUpdated,
    int FridgeUnchanged,
    int CookingSessionsAdded,
    int CookingSessionsUpdated,
    int CookingSessionsUnchanged,
    int CompletedCookingSessionConflicts
) {
    public int IncomingFridgeCount => FridgeAdded + FridgeUpdated + FridgeUnchanged;

    public int IncomingCookingSessionCount =>
        CookingSessionsAdded
        + CookingSessionsUpdated
        + CookingSessionsUnchanged
        + CompletedCookingSessionConflicts;

    public bool HasChanges =>
        FridgeAdded > 0
        || FridgeUpdated > 0
        || CookingSessionsAdded > 0
        || CookingSessionsUpdated > 0;
}
