using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;

namespace CalorieLedger.Application.Tests.Products;

public sealed class ProductCatalogServiceTests {
    [Fact]
    public void Save_ValidDraft_PersistsNormalizedProduct() {
        var store = new InMemoryProductCatalogStore();

        var service = new ProductCatalogService(store);

        var draft = service.CreateNew() with {
            Name = "  Творог 5%  ",
            NutritionBasis = NutritionBasis.Per100Grams,
            CaloriesKcal = 121m,
            ProteinG = 17m,
            FatG = 5m,
            CarbsG = 3m,
            Brand = "  Простоквашино  ",
            Barcode = "  4600000000001  ",
        };

        var result = service.Save(draft);

        Assert.True(result.IsSuccess);

        var saved = Assert.IsType<ProductCatalogItem>(store.Get(draft.Id));

        Assert.Equal(
            "Творог 5%",
            saved.Name
        );

        Assert.Equal(
            "Простоквашино",
            saved.Brand
        );

        Assert.Equal(
            "4600000000001",
            saved.Barcode
        );

        Assert.Equal(
            NutritionBasis.Per100Grams,
            saved.Nutrition.Basis
        );

        Assert.Equal(
            121m,
            saved.Nutrition.CaloriesKcal
        );
    }

    [Fact]
    public void Save_InvalidDraft_ReturnsAllRelevantErrorsAndDoesNotPersist() {
        var store = new InMemoryProductCatalogStore();

        var service = new ProductCatalogService(store);

        var draft = new ProductCatalogDraft(
            Id: Guid.Empty,
            Name: " ",
            NutritionBasis: 0,
            CaloriesKcal: -1m,
            ProteinG: -2m,
            FatG: -3m,
            CarbsG: -4m
        );

        var result = service.Save(draft);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            ProductCatalogValidationError.MissingId,
            result.Errors
        );

        Assert.Contains(
            ProductCatalogValidationError.MissingName,
            result.Errors
        );

        Assert.Contains(
            ProductCatalogValidationError.InvalidNutritionBasis,
            result.Errors
        );

        Assert.Contains(
            ProductCatalogValidationError.InvalidCalories,
            result.Errors
        );

        Assert.Contains(
            ProductCatalogValidationError.InvalidProtein,
            result.Errors
        );

        Assert.Contains(
            ProductCatalogValidationError.InvalidFat,
            result.Errors
        );

        Assert.Contains(
            ProductCatalogValidationError.InvalidCarbs,
            result.Errors
        );

        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Search_Query_MatchesNameBrandAndBarcodeCaseInsensitively() {
        var store = new InMemoryProductCatalogStore();

        var service = new ProductCatalogService(store);

        var byName = CreateProduct(
            "Greek Yogurt",
            brand: "Brand A",
            barcode: "111"
        );

        var byBrand = CreateProduct(
            "Творог",
            brand: "Milko",
            barcode: "222"
        );

        var byBarcode = CreateProduct(
            "Кефир",
            brand: "Brand C",
            barcode: "4601234567890"
        );

        store.Save(byName);

        store.Save(byBrand);

        store.Save(byBarcode);

        Assert.Equal(
            byName,
            Assert.Single(
                service.Search("yOgUrT")
            )
        );

        Assert.Equal(
            byBrand,
            Assert.Single(
                service.Search("MILKO")
            )
        );

        Assert.Equal(
            byBarcode,
            Assert.Single(
                service.Search("4567890")
            )
        );
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsCatalogInStableOrder() {
        var store = new InMemoryProductCatalogStore();

        var service = new ProductCatalogService(store);

        var later = CreateProduct("Яблоко");

        var earlier = CreateProduct("Банан");

        store.Save(later);

        store.Save(earlier);

        var result = service.Search(null);

        Assert.Equal(
            earlier,
            result[0]
        );

        Assert.Equal(
            later,
            result[1]
        );
    }

    [Fact]
    public void Load_ExistingProduct_ReturnsEditableDraft() {
        var store = new InMemoryProductCatalogStore();

        var product = CreateProduct(
            "Молоко",
            brand: "Test",
            barcode: "123"
        );

        store.Save(product);

        var service = new ProductCatalogService(store);

        var draft = Assert.IsType<ProductCatalogDraft>(service.Load(product.Id));

        Assert.Equal(
            product.Id,
            draft.Id
        );

        Assert.Equal(
            product.Name,
            draft.Name
        );

        Assert.Equal(
            product.Brand,
            draft.Brand
        );

        Assert.Equal(
            product.Barcode,
            draft.Barcode
        );

        Assert.Equal(
            product.Nutrition,
            new NutritionFacts(
                Basis: draft.NutritionBasis,
                CaloriesKcal: draft.CaloriesKcal,
                ProteinG: draft.ProteinG,
                FatG: draft.FatG,
                CarbsG: draft.CarbsG
            )
        );
    }

    [Fact]
    public void Delete_ExistingProduct_RemovesIt() {
        var store = new InMemoryProductCatalogStore();

        var product = CreateProduct("Молоко");

        store.Save(product);

        var service = new ProductCatalogService(store);

        var deleted = service.Delete(product.Id);

        Assert.True(deleted);

        Assert.Null(store.Get(product.Id));
    }

    private static ProductCatalogItem CreateProduct(
        string name,
        string? brand = null,
        string? barcode = null
    ) {
        return new ProductCatalogItem(
            Id: Guid.NewGuid(),
            Name: name,
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 100m,
                ProteinG: 10m,
                FatG: 5m,
                CarbsG: 3m
            ),
            Brand: brand,
            Barcode: barcode
        );
    }

    [Fact]
    public void Save_TotalNutritionBasis_ReturnsValidationError() {
        var store = new InMemoryProductCatalogStore();

        var service = new ProductCatalogService(store);

        var draft = service.CreateNew() with {
            Name = "Готовая порция",
            NutritionBasis = NutritionBasis.Total,
            CaloriesKcal = 500m,
        };

        var result = service.Save(draft);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            ProductCatalogValidationError.InvalidNutritionBasis,
            result.Errors
        );

        Assert.Empty(store.GetAll());
    }
}
