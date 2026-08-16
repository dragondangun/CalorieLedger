using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonProductCatalogStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonProductCatalogStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );

        filePath = Path.Combine(
            directoryPath,
            "product-catalog.json"
        );
    }

    [Fact]
    public void Save_Product_PersistsAcrossStoreInstances() {
        var product = CreateProduct("Творог");

        var firstStore = new JsonProductCatalogStore(filePath);

        firstStore.Save(product);

        var secondStore = new JsonProductCatalogStore(filePath);

        Assert.Equal(
            product,
            secondStore.Get(
                product.Id
            )
        );
    }

    [Fact]
    public void Save_ExistingId_UpdatesPersistedProduct() {
        var product = CreateProduct("Творог");

        var firstStore = new JsonProductCatalogStore(filePath);

        firstStore.Save(product);

        firstStore.Save(
            product with {
                Name = "Творог 5%",
                Brand = "Test brand",
            }
        );

        var secondStore = new JsonProductCatalogStore(filePath);

        var saved = Assert.IsType<ProductCatalogItem>(secondStore.Get(product.Id));

        Assert.Equal(
            "Творог 5%",
            saved.Name
        );

        Assert.Equal(
            "Test brand",
            saved.Brand
        );

        Assert.Single(secondStore.GetAll());
    }

    [Fact]
    public void Delete_Product_PersistsDeletion() {
        var product = CreateProduct("Творог");

        var firstStore = new JsonProductCatalogStore(filePath);

        firstStore.Save(product);

        Assert.True(firstStore.Delete(product.Id));

        var secondStore = new JsonProductCatalogStore(filePath);

        Assert.Null(secondStore.Get(product.Id));

        Assert.Empty(secondStore.GetAll());
    }

    [Fact]
    public void GetAll_UnorderedProducts_ReturnsStableOrder() {
        var store = new JsonProductCatalogStore(filePath);

        var apple = CreateProduct("Яблоко");

        var banana = CreateProduct("Банан");

        store.Save(apple);

        store.Save(banana);

        var result = store.GetAll();

        Assert.Equal(
            banana,
            result[0]
        );

        Assert.Equal(
            apple,
            result[1]
        );
    }

    [Fact]
    public void GetAll_CorruptedJson_PreservesFileAndReturnsEmpty() {
        Directory.CreateDirectory(directoryPath);

        File.WriteAllText(
            filePath,
            "{ invalid json"
        );

        var store = new JsonProductCatalogStore(filePath);

        var result = store.GetAll();

        Assert.Empty(result);

        Assert.False(File.Exists(filePath));

        var preservedFiles = Directory.GetFiles(
            directoryPath,
            "product-catalog.json.corrupt-*"
        );

        Assert.Single(preservedFiles);
    }

    private static ProductCatalogItem CreateProduct(string name) {
        return new ProductCatalogItem(
            Id: Guid.NewGuid(),
            Name: name,
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 120m,
                ProteinG: 17m,
                FatG: 5m,
                CarbsG: 3m
            ),
            Brand: null,
            Barcode: null
        );
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
