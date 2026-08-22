namespace CalorieLedger.Application.Sync;

public sealed class InMemorySyncDeviceIdentityStore:ISyncDeviceIdentityStore {
    private SyncDeviceIdentity? identity;

    public InMemorySyncDeviceIdentityStore(
        SyncDeviceIdentity? identity = null
    ) {
        this.identity = identity;
    }

    public SyncDeviceIdentity GetOrCreate() {
        identity ??= new SyncDeviceIdentity(Guid.NewGuid());
        return identity;
    }
}
