namespace CalorieLedger.Application.Sync;

public sealed record SyncSnapshotParseResult(
    bool IsSuccess,
    SyncSnapshot? Snapshot,
    IReadOnlyList<SyncSnapshotParseError> Errors
);
