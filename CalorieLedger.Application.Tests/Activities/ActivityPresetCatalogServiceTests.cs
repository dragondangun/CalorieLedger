using CalorieLedger.Application.Activities;

namespace CalorieLedger.Application.Tests.Activities;

public sealed class ActivityPresetCatalogServiceTests {
    [Fact]
    public void Save_CustomPreset_AppearsAlongsideBuiltInPresets() {
        var service = new ActivityPresetCatalogService(new InMemoryActivityPresetStore());
        var draft = service.CreateNew() with { Name = "HEMA, спарринги", MetValue = 7m };

        var result = service.Save(draft);

        Assert.True(result.IsSuccess);

        var saved = Assert.Single(service.GetCustom());
        Assert.Equal("HEMA, спарринги", saved.Name);
        Assert.Equal(7m, saved.MetValue);
        Assert.False(saved.IsBuiltIn);

        Assert.Contains(service.GetAll(), preset => preset.IsBuiltIn);
        Assert.Contains(service.GetAll(), preset => preset.Code == saved.Code);
    }

    [Fact]
    public void Save_DuplicateBuiltInName_IsRejected() {
        var service = new ActivityPresetCatalogService(new InMemoryActivityPresetStore());

        var result = service.Save(
            service.CreateNew() with {
                Name = BuiltInActivityPresetCatalog.All[0].Name,
                MetValue = 5m
            }
        );

        Assert.False(result.IsSuccess);
        Assert.Contains(ActivityPresetValidationError.DuplicateName, result.Errors);
    }

    [Fact]
    public void Save_ExistingCustomPreset_UpdatesPreset() {
        var service = new ActivityPresetCatalogService(new InMemoryActivityPresetStore());
        var draft = service.CreateNew() with { Name = "HEMA", MetValue = 6m };

        Assert.True(service.Save(draft).IsSuccess);

        var updated = draft with { Name = "HEMA, интенсивно", MetValue = 8m };

        Assert.True(service.Save(updated).IsSuccess);

        var saved = Assert.Single(service.GetCustom());
        Assert.Equal("HEMA, интенсивно", saved.Name);
        Assert.Equal(8m, saved.MetValue);
    }

    [Fact]
    public void Delete_BuiltInPreset_IsRejected() {
        var service = new ActivityPresetCatalogService(new InMemoryActivityPresetStore());
        var builtIn = BuiltInActivityPresetCatalog.All[0];

        Assert.False(service.Delete(builtIn.Code));
        Assert.NotNull(service.Find(builtIn.Code));
    }
}
