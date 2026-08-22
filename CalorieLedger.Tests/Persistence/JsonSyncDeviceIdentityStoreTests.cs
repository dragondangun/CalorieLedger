using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonSyncDeviceIdentityStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonSyncDeviceIdentityStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );
        filePath = Path.Combine(directoryPath, "sync-device.json");
    }

    [Fact]
    public void GetOrCreate_PersistsSameDeviceIdentityAcrossStoreInstances() {
        var firstStore = new JsonSyncDeviceIdentityStore(filePath);
        var firstIdentity = firstStore.GetOrCreate();

        var secondStore = new JsonSyncDeviceIdentityStore(filePath);
        var secondIdentity = secondStore.GetOrCreate();

        Assert.NotEqual(Guid.Empty, firstIdentity.Id);
        Assert.Equal(firstIdentity, secondIdentity);
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(
                directoryPath,
                recursive: true
            );
        }
    }
}
