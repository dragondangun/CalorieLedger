using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;
using CalorieLedger.ViewModels.Products;

namespace CalorieLedger.Tests.ViewModels.Products;

public sealed class ProductCatalogManagerViewModelTests {
    [Fact]
    public void Constructor_ExistingProducts_LoadsCatalog() {
        var store = new InMemoryProductCatalogStore();

        var product = CreateProduct("Творог");

        store.Save(product);

        var viewModel = CreateViewModel(store);

        var item = Assert.Single(viewModel.Products);

        Assert.Equal(
            product.Id,
            item.Id
        );

        Assert.Equal(
            "Творог",
            item.Name
        );
    }

    [Fact]
    public void SearchQuery_FiltersCatalog() {
        var store = new InMemoryProductCatalogStore();

        store.Save(
            CreateProduct(
                "Творог",
                brand: "Молочная ферма"
            )
        );

        store.Save(
            CreateProduct(
                "Кефир",
                brand: "Другой бренд"
            )
        );

        var viewModel = CreateViewModel(store);

        viewModel.SearchQuery = "ферма";

        var result = Assert.Single(viewModel.Products);

        Assert.Equal(
            "Творог",
            result.Name
        );
    }

    [Fact]
    public void AddProduct_Save_CreatesProductAndRefreshesCatalog() {
        var store = new InMemoryProductCatalogStore();

        var viewModel = CreateViewModel(store);

        viewModel.AddProductCommand.Execute(null);

        var editor = Assert.IsType<ProductCatalogEditorViewModel>(viewModel.Editor);

        editor.Name = "Кефир";

        editor.CaloriesKcal = 53m;
        editor.ProteinG = 3m;
        editor.FatG = 2.5m;
        editor.CarbsG = 4m;

        editor.NutritionBasis = NutritionBasis.Per100Milliliters;
        editor.Brand = "Test";
        editor.Barcode = "4601234567890";

        editor.SaveCommand.Execute(null);

        Assert.Null(viewModel.Editor);

        var product = Assert.Single(store.GetAll());

        Assert.Equal(
            "Кефир",
            product.Name
        );

        Assert.Equal(
            "Test",
            product.Brand
        );

        Assert.Equal(
            "4601234567890",
            product.Barcode
        );

        Assert.Single(viewModel.Products);
    }

    [Fact]
    public void EditProduct_Save_UpdatesExistingProduct() {
        var store = new InMemoryProductCatalogStore();

        var product = CreateProduct("Творог");

        store.Save(product);

        var viewModel = CreateViewModel(store);

        var item = Assert.Single(viewModel.Products);

        item.EditCommand.Execute(null);

        var editor = Assert.IsType<ProductCatalogEditorViewModel>(viewModel.Editor);

        Assert.Equal(
            "Творог",
            editor.Name
        );

        editor.Name = "Творог 5%";

        editor.Brand = "Новый бренд";

        editor.SaveCommand.Execute(null);

        var saved = Assert.IsType<ProductCatalogItem>(store.Get(product.Id));

        Assert.Equal(
            "Творог 5%",
            saved.Name
        );

        Assert.Equal(
            "Новый бренд",
            saved.Brand
        );

        Assert.Equal(
            "Творог 5%",
            Assert.Single(viewModel.Products).Name
        );
    }

    [Fact]
    public void DeleteProduct_Confirmed_RemovesProduct() {
        var store = new InMemoryProductCatalogStore();

        var product = CreateProduct("Творог");

        store.Save(product);

        var viewModel = CreateViewModel(store);

        var item = Assert.Single(viewModel.Products);

        item.DeleteCommand.Execute(null);

        Assert.True(item.IsDeleteConfirmationVisible);

        item.ConfirmDeleteCommand.Execute(null);

        Assert.Empty(store.GetAll());

        Assert.Empty(viewModel.Products);

        Assert.True(viewModel.HasNoProducts);
    }

    private static ProductCatalogManagerViewModel CreateViewModel(IProductCatalogStore store) {
        return new ProductCatalogManagerViewModel(
            new ProductCatalogService(store),
            onClosed: () => { }
        );
    }

    private static ProductCatalogItem CreateProduct(
        string name,
        string? brand = null
    ) {
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
            Brand: brand
        );
    }
}
