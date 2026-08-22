using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Sync;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels.Sync;

namespace CalorieLedger.Tests.ViewModels.Sync;

public sealed class SyncManagerViewModelTests {
    [Fact]
    public void ValidateImport_ValidRemoteSnapshot_ShowsPreviewAndEnablesApply() {
        var localService = CreateService(Guid.NewGuid());
        var remoteFridge = new InMemoryFridgeStore();
        remoteFridge.Save(CreateFridgeItem("Кефир"));
        var remoteService = CreateService(Guid.NewGuid(), remoteFridge);
        var viewModel = new SyncManagerViewModel(
            localService,
            onClosed: () => { }
        );

        viewModel.ImportText = remoteService.CreateExport();
        viewModel.ValidateImportCommand.Execute(null);

        Assert.True(viewModel.HasPreview);
        Assert.True(viewModel.CanApply);
        Assert.Contains("новых 1", viewModel.PreviewSummary);
        Assert.Contains("можно применить", viewModel.ActionSummary);
    }

    [Fact]
    public void ImportTextChanged_InvalidatesPreviouslyValidatedSnapshot() {
        var localService = CreateService(Guid.NewGuid());
        var remoteService = CreateService(Guid.NewGuid());
        var viewModel = new SyncManagerViewModel(
            localService,
            onClosed: () => { }
        );

        viewModel.ImportText = remoteService.CreateExport();
        viewModel.ValidateImportCommand.Execute(null);

        Assert.True(viewModel.CanApply);

        viewModel.ImportText += " ";

        Assert.False(viewModel.HasPreview);
        Assert.False(viewModel.CanApply);
        Assert.Empty(viewModel.PreviewSummary);
    }

    [Fact]
    public void ApplyImport_MergesDataAndInvokesAppliedCallback() {
        var localFridge = new InMemoryFridgeStore();
        var localService = CreateService(Guid.NewGuid(), localFridge);
        var remoteFridge = new InMemoryFridgeStore();
        var remoteItem = CreateFridgeItem("Творог");
        remoteFridge.Save(remoteItem);
        var remoteService = CreateService(Guid.NewGuid(), remoteFridge);
        var appliedCount = 0;
        var viewModel = new SyncManagerViewModel(
            localService,
            onClosed: () => { },
            onApplied: () => appliedCount++
        );

        viewModel.ImportText = remoteService.CreateExport();
        viewModel.ValidateImportCommand.Execute(null);
        viewModel.ApplyImportCommand.Execute(null);

        Assert.NotNull(localFridge.Get(remoteItem.Id));
        Assert.Equal(1, appliedCount);
        Assert.False(viewModel.CanApply);
        Assert.Contains("Синхронизация применена", viewModel.ActionSummary);
    }

    private static SyncSnapshotService CreateService(
        Guid deviceId,
        InMemoryFridgeStore? fridgeStore = null
    ) {
        return new SyncSnapshotService(
            fridgeStore ?? new InMemoryFridgeStore(),
            new InMemoryCookingSessionStore(),
            new InMemoryCookingBatchStore(),
            new InMemorySyncDeviceIdentityStore(
                new SyncDeviceIdentity(deviceId)
            )
        );
    }

    private static FridgeItem CreateFridgeItem(string name) {
        return new FridgeItem(
            Id: Guid.NewGuid(),
            Name: name,
            Quantity: FoodQuantity.Grams(500m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 100m,
                ProteinG: 10m,
                FatG: 5m,
                CarbsG: 4m
            )
        );
    }
}
