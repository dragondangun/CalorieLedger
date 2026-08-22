namespace CalorieLedger.Application.Sync;

public interface ISyncDeviceIdentityStore {
    SyncDeviceIdentity GetOrCreate();
}
