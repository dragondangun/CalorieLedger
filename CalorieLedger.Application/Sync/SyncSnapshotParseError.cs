namespace CalorieLedger.Application.Sync;

public enum SyncSnapshotParseError {
    EmptyInput,
    InvalidJson,
    UnsupportedProtocol,
    MissingSnapshotId,
    MissingSourceDeviceId,
    OwnDeviceSnapshot,
    DuplicateFridgeItem,
    DuplicateCookingSession,
    InvalidFridgeItem,
    InvalidCookingSession,
}
